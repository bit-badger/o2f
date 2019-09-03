namespace Cinco

open Aether
open Freya.Core
open MiniGuids
open Freya.Optics.Http
open Freya.Optics.Http.Cors
open Freya.Types.Http.State
open Raven.Client.Documents
open Raven.Client.Documents.Session
open System
open System.Collections.Generic

/// Required public functionality for a session
type ISession =
  /// Try to get a value from the session, returning an option if it does not exist
  abstract TryGet : string -> Freya<string option>
  /// Set a value in the session
  abstract Set : string -> string -> Freya<unit>
  /// Eliminate the user's session
  abstract Destroy : unit -> Freya<unit>


/// The document in which we will store our session data
[<CLIMutable>]
type SessionDocument =
  { Id         : string
    Expiration : DateTime
    Data       : IDictionary<string, string>
    }
    

/// The session store implementation
type Session (store : IDocumentStore) =
  
  /// The name of the cookie in which we'll store our session ID
  let cookieName = Name ".Cinco.Session"

  /// Expiration for the session (1 hour)
  let expiry = TimeSpan (1, 0, 0)

  /// Shorthand for making a session document ID
  let sessDocId = sprintf "Sessions/%s"

  /// Create a response cookie with the given session ID and expiration
  let responseCookie sessId expires =
    SetCookie ((Pair (cookieName, Value sessId)), Attributes [ Expires expires; HttpOnly ])

  /// Get the max age of a session
  let maxAge () = DateTime.UtcNow + expiry
  
  /// Add a response cookie with the session ID
  let addResponseCookie sessId =
    freya {
      match! Freya.Optic.get Response.Headers.setCookie_ with
      | Some _ -> ()
      | None -> do! Freya.Optic.set Response.Headers.setCookie_ (responseCookie sessId (maxAge ()) |> Some)
      }

  /// Create a session, returning the session ID
  let createSessionId =
    freya {
      let sessId = (MiniGuid.NewGuid >> string) ()
      do! addResponseCookie sessId
      return sessId
      }

  /// Get the ID of the current session (creating one if one does not exist)
  let getSessionId =
    freya {
      match! Freya.Optic.get (Request.Headers.cookie_) with
      | Some c ->
          let theCookie =
            (fst Cookie.pairs_) c
            |> List.tryFind (fun p -> Optic.get Pair.name_ p = cookieName)
          match theCookie with
          | Some p ->
              let (Value value) = Optic.get Pair.value_ p
              return value
          | None -> return! createSessionId
      | None -> return! createSessionId
      }
  
  /// Get the session document, handling expired documents
  let getSessionDoc sessId (docSession : IDocumentSession) =
    let sessDoc = docSession.Load<SessionDocument> (sessDocId sessId) |> (box >> Option.ofObj)
    match sessDoc with
    | Some doc ->
        let d = unbox<SessionDocument> doc
        match d.Expiration < DateTime.UtcNow with
        | true -> 
            docSession.Delete (sessDocId sessId)
            docSession.SaveChanges ()
            None
        | false -> Some d
    | None -> None

  interface ISession with

    member __.TryGet name =
      let docSession = store.OpenSession ()
      try
        freya {
          let! sessionId = getSessionId
          return
            match getSessionDoc sessionId docSession with
            | Some doc -> match doc.Data.ContainsKey name with true -> Some doc.Data.[name] | false -> None
            | None -> None
          }
      finally
        docSession.Dispose ()
    
    member __.Set name item =
      let docSession = store.OpenSession ()
      try
        freya {
          let! sessionId = getSessionId
          match getSessionDoc sessionId docSession with
          | Some doc ->
              doc.Data.[name] <- item
              docSession.Advanced.Patch (sessDocId sessionId, (fun x -> x.Data),       doc.Data)
              docSession.Advanced.Patch (sessDocId sessionId, (fun x -> x.Expiration), maxAge ())
              docSession.Advanced.IgnoreChangesFor doc
          | None ->
              { Id         = sessDocId sessionId
                Expiration = maxAge ()
                Data       = [ name, item ] |> dict
                }
              |> docSession.Store
          docSession.SaveChanges ()
          do! addResponseCookie sessionId
          }
      finally
        docSession.Dispose ()
    
    member __.Destroy () =
      let docSession = store.OpenSession ()
      try
        freya {
          let! sessionId = getSessionId
          docSession.Delete (sessDocId sessionId)
          docSession.SaveChanges ()
          // Expire the cookie
          do! Freya.Optic.set Response.Headers.setCookie_ (responseCookie sessionId DateTime.UnixEpoch |> Some)
          }
      finally
        docSession.Dispose ()
