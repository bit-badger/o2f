namespace Cinco

// -- begin code lifted from #er demo --
type ReaderM<'d, 'out> = 'd -> 'out

module Reader =
  // basic operations
  let run dep (rm : ReaderM<_,_>) = rm dep
  let constant (c : 'c) : ReaderM<_,'c> = fun _ -> c
  // lifting of functions and state
  let lift1 (f : 'd -> 'a -> 'out) : 'a -> ReaderM<'d, 'out> = fun a dep -> f dep a
  let lift2 (f : 'd -> 'a -> 'b -> 'out) : 'a -> 'b -> ReaderM<'d, 'out> = fun a b dep -> f dep a b
  let lift3 (f : 'd -> 'a -> 'b -> 'c -> 'out) : 'a -> 'b -> 'c -> ReaderM<'d, 'out> = fun a b c dep -> f dep a b c
  let liftDep (proj : 'd2 -> 'd1) (rm : ReaderM<'d1, 'output>) : ReaderM<'d2, 'output> = proj >> rm
  // functor
  let fmap (f : 'a -> 'b) (g : 'c -> 'a) : ('c -> 'b) = g >> f
  let map (f : 'a -> 'b) (rm : ReaderM<'d, 'a>) : ReaderM<'d,'b> = rm >> f
  let (<?>) = map
  // applicative-functor
  let apply (f : ReaderM<'d, 'a->'b>) (rm : ReaderM<'d, 'a>) : ReaderM<'d, 'b> =
    fun dep ->
      let f' = run dep f
      let a  = run dep rm
      f' a
  let (<*>) = apply
  // monad
  let bind (rm : ReaderM<'d, 'a>) (f : 'a -> ReaderM<'d,'b>) : ReaderM<'d, 'b> =
    fun dep ->
      f (rm dep) 
      |> run dep 
  let (>>=) = bind
  type ReaderMBuilder internal () =
    member __.Bind(m, f)    = m >>= f
    member __.Return(v)     = constant v
    member __.ReturnFrom(v) = v
    member __.Delay(f)      = f ()
  let reader = ReaderMBuilder()
// -- end code lifted from #er demo --

open Raven.Client.Documents
open FreyaSessionProvider.Types
open System.Security.Cryptography

type IDependencies =
  abstract Store : IDocumentStore
  abstract Session : ISessionProvider

[<AutoOpen>]
module DependencyExtraction =
  
  let getStore   (deps : IDependencies) = deps.Store
  let getSession (deps : IDependencies) = deps.Session

[<AutoOpen>]
module Dependencies =
  
  open Data
  open FreyaSessionProvider
  open FreyaSessionProvider.RavenDB
  open System
  open System.IO
  
  let private cfg = (File.ReadAllText >> DataConfig.fromJson) "data-config.json"
  let private depSingle = lazy (
    { new IDependencies with
        member __.Store
          with get () =
            let store = lazy (
              let stor = DataConfig.configureStore cfg (new DocumentStore ())
              stor.Conventions.CustomizeJsonSerializer <-
                fun x -> Converters.all |> List.ofSeq |> List.iter x.Converters.Add
              stor.Initialize ()
              )
            store.Force()
        member this.Session
          with get () =
            let aes = Aes.Create()
            let testKey = "a8VXHmTNXhfArTUCunP5bb2DKRbRicm6asutQCHFg/k="
            let testIV = "NuEC6eKxuEREh2S2MrETSA=="
            aes.Key <- Convert.FromBase64String testKey
            aes.IV <- Convert.FromBase64String testIV
            SessionProvider.Create
              { SessionProviderConfig.defaults with
                  store  = RavenDBSessionStore this.Store
                  crypto = aes }
            //upcast Session this.Store
      })
  let deps = depSingle.Force ()
