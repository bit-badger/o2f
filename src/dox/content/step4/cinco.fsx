(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Chiron/lib/netstandard2.0/Chiron.dll"
#r "../../../packages/Freya.Core/lib/netstandard2.0/Freya.Core.dll"
#r "../../../packages/Freya.Machines.Http/lib/netstandard2.0/Freya.Machines.Http.dll"
#r "../../../packages/Freya.Routers.Uri.Template/lib/netstandard2.0/Freya.Routers.Uri.Template.dll"
#r "../../../packages/Freya.Types.Uri.Template/lib/netstandard2.0/Freya.Types.Uri.Template.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Owin/lib/netstandard2.0/Microsoft.AspNetCore.Owin.dll"
#r "../../../packages/Microsoft.AspNetCore.Server.Kestrel/lib/netstandard2.0/Microsoft.AspNetCore.Server.Kestrel.dll"
#r "../../../packages/Microsoft.FSharpLu.Json/lib/netstandard2.0/Microsoft.FSharpLu.Json.dll"
#r "../../../packages/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../../packages/RavenDb.Client/lib/netstandard2.0/Raven.Client.dll"

namespace Cinco

open Raven.Client.Documents

module Data =
  type ConfigParameter =
    | Url      of string
    | Database of string
  type DataConfig = { Parameters : ConfigParameter list }
  module DataConfig =
    let fromJson (json : string) : DataConfig = { Parameters = [] }
    let configureStore config store  =
      config.Parameters
      |> List.fold
          (fun (stor : DocumentStore) par -> 
              match par with
              | Url url -> stor.Urls <- [| url |]; stor
              | Database db -> stor.Database <- db; stor)
          store
  /// JSON converters for custom types
  module Converters =
    open Newtonsoft.Json
    let all : JsonConverter seq = Seq.empty

type IDependencies =
  abstract Store : IDocumentStore
(**
### Cinco - Step 4

#### A Note about Sessions

In each project up to this point, one of the things we've done is set up sessions. These session stores are integrated
into the ASP.NET Core pipeline or Nancy's SDHP - but if you do a search for "Freya sessions," you'll pretty much come up
with links to YouTube for live sessions by an artist of the same name. There is no "session provider" for Freya because
it is based on RFCs. _(It also uses the OWIN pipeline, not the ASP.NET Core pipeline, so we can't just piggyback off
that one.)_

However, this is not the problem we may think it to be. There are several ways to maintain state in an HTML application,
including cookies, script, and local storage. In fact, the
[original RFC for cookies](https://tools.ietf.org/html/rfc2109) specifically calls out sessions as one of the intended
uses for cookies. Both ASP.NET Core and Nancy's "sessions" are a two part solution, with a "session cookie" (containing
just the identifier for the session) and a server-side repository of data tied to that identifier. We can easily
implement our own session store, and since cookies **are** supported in Freya, we can make our session store
Freya-aware.

Going into a lot of detail about the implementation is a rabbit trail we won't take; however, if you look at
[`Sessions.fs` in the checkpoint for this step](https://github.com/bit-badger/o2f/tree/master/src/5-Cinco/Sessions.fs),
you can see the basic implementation. There are many ways it could be improved (hard-coded values being made
configurable would be a big one), but it will be what we use for our project moving forward.

#### Dependencies

Since we don't need to add a session provider, we only need to add `MiniGuid` and `TaskBuilder.fs`. Add those to
`paket.references` and run `paket install`.

#### Previously Written Code

We can plagiarize* significant parts of **Quatro**. We'll bring across the `Converters` module (adding it to `Data.fs`),
we'll replace the DU modules with the `MiniGuid`-based ones in `Domain.fs`, and we'll reuse most of the `seed` function
(more on that in a bit).

#### `App.fs` and Other File Changes

Up to this point, we've put the majority of our code in `App.fs`. While F# solutions have many fewer files than C#
solutions do, splitting some things out will help us in the future. First up, move the definitions of `cfg` and `deps`
from here to `Dependencies.fs`, as part of a new auto-opened `Dependencies` module.
*)
[<AutoOpen>]
module Dependencies =
  
  open Data
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
      })
  let deps = depSingle.Force ()
(**
We also need to move `Dependencies.fs` in the build order below `Data.fs`, since we rely on it for our data config and
our collection of JSON converters that we use to initialize RavenDB.

Next, let's create `Handlers.fs` to hold all our Freya functions that will directly map to URLs. Add it to the build
order just below dependencies, and have its first line be `module Cinco.Handlers`. Move the `hello` function from the
`WebApp` module there, then remove the `WebApp` module. At this point, if you want to get rid of the build error, change
`WebApp.hello` to `Handlers.hello` in the definition of `freyaOwin`.

// TODO: stopped here; will pick up here once the session store is done

---
[Back to Step 4](../step4)

\* - It's OK to plagiarize yourself...
*)