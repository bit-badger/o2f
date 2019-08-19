namespace Cinco

open Aether
open Freya.Core
open MiniGuids
open Freya.Optics.Http
open Freya.Optics.Http.Cors
open Freya.Types.Http.State
open Raven.Client.Documents
open System
open System.Collections.Generic

/// Required public functionality for a session
type ISession =
  /// Try to get a value from the session, returning an option if it does not exist
  abstract TryGet<'T> : string -> Freya<'T option>
  /// Set a value in the session
  abstract Set<'T> : string -> 'T -> Freya<unit>
  /// Eliminate the user's session
  abstract Destroy : unit -> Freya<unit>


/// The document in which we will store our session data
[<CLIMutable>]
type SessionDocument =
  { Id         : string
    Expiration : DateTime
    Data       : IDictionary<string, obj>
    }
    

/// The session store implementation
type Session (store : IDocumentStore) =
  
  /// The name of the cookie in which we'll store our session ID
  let cookieName = Name ".Cinco.Session"

  /// Expiration for the session (1 hour)
  let expiry = TimeSpan (1, 0, 0)

  /// Shorthand for making a session document ID
  let sessDocId = sprintf "Sessions/%s"

  /// Create a session, returning the session ID
  let createSessionId =
    freya {
      let sessId = (MiniGuid.NewGuid >> string) ()
      // TODO: add a response cookie with this ID
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
  
  interface ISession with

    member __.TryGet<'T> name =
      freya {
        let! sessionId = getSessionId
        let docSession = store.OpenSession ()
        let dispose () = docSession.Dispose ()
        let sessDoc = docSession.Load<SessionDocument> (sessDocId sessionId) |> (box >> Option.ofObj)
        match sessDoc with
        | Some doc ->
            let d = unbox<SessionDocument> doc
            match d.Expiration < DateTime.Now with
            | true -> dispose (); return None
            | false ->
                match d.Data.ContainsKey name with
                | true -> dispose (); return Some (d.Data.[name] :?> 'T)
                | false -> dispose (); return None
        | None -> dispose (); return None
        }
    
    member __.Set<'T> name (item : 'T) =
      freya {
        let! sessionId = getSessionId
        let docSession = store.OpenSession ()
        let dispose () = docSession.Dispose ()
        let sessDoc = docSession.Load<SessionDocument> (sessDocId sessionId) |> (box >> Option.ofObj)
        let addDoc () =
          { Id         = sessDocId sessionId
            Expiration = DateTime.Now + expiry
            Data       = [ name, item :> obj ] |> dict
            }
          |> docSession.Store
          docSession.SaveChanges ()
          dispose ()
        match sessDoc with
        | Some doc ->
            let d = unbox<SessionDocument> doc
            match d.Expiration < DateTime.Now with
            | true -> addDoc ()
            | false ->
                d.Data.[name] <- item
                docSession.Advanced.Patch (sessDocId sessionId, (fun x -> x.Data),       d.Data)
                docSession.Advanced.Patch (sessDocId sessionId, (fun x -> x.Expiration), DateTime.Now + expiry)
                docSession.SaveChanges ()
                dispose ()
        | None -> addDoc ()
        // TODO: make sure the response header has the session cookie
        }
    
    member __.Destroy () =
      freya {
        let! sessionId = getSessionId
        let docSession = store.OpenSession ()
        docSession.Delete (sessDocId sessionId)
        docSession.SaveChanges ()
        docSession.Dispose ()
        // TODO: expire the session cookie in the response
      }
