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

> #### A Note about Sessions
>
> In each project up to this point, one of the things we've done is set up sessions. These session stores are integrated
> into the ASP.NET Core pipeline or Nancy's SDHP - but when I did a search for "Freya sessions," I pretty much just came
> up with links to YouTube for live sessions by an artist of the same name. Prior to the time of me writing this step,
> there was no "session provider" for Freya; as the project is based on RFCs, it handles cookies, but not a persistent
> store based off a value from that cookie. _(It also uses the OWIN pipeline instead of the ASP.NET Core pipeline, so we
> couldn't just piggyback off that one.)_
>
> Before completing this step, I wrote
> [a session provider for Freya](https://github.com/bit-badger/FreyaSessionProvider), backed by RavenDB. Mimicking how
> [my Nancy session providers](https://github.com/danieljsummers/Nancy.Session.Persistable) work, it's written to
> support several different persistence technologies, which I plan to write once this step is done. Diving into the
> details of this project would be a confusing detour; however, if you want to read the code, it's linked above.
>
> I do want to encourage you, dear reader, to go beyond the syntax to learn the underlying concepts. I've never thought
> of myself as a "session guy," but with this provider, I have now written the session providers used in all five of our
> projects. What made this possible was understanding how HTTP requests are handled. Once you figure out that a session
> is just an ID in a cookie, handling the persistence server-side begins to make sense; and, once you understand how an
> in-memory provider works, all that's left is making each action apply to a database, a flat file, or whatever
> persistence you like.
>
> Read up on the concepts, and write the naïve implementations; it's the best way to learn things deeply.

#### Dependencies

We'll add `FreyaSessionProvider.RavenDB`, `MiniGuid`, and `TaskBuilder.fs` to `paket.references` and run
`paket install`. This will bring in the dependencies we need.

#### Previously Written Code

We can plagiarize* significant parts of **Quatro**. We'll bring across the `Converters` module (adding it to `Data.fs`),
we'll replace the DU modules with the `MiniGuid`-based ones in `Domain.fs`, and we'll reuse most of the `seed` function
(more on that in a bit).

#### `App.fs` and Other File Changes

// TODO: stopped here - WIP below, quite possibly incorrect at this point...

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

// TODO: continue

---
[Back to Step 4](../step4)

\* - It's OK to plagiarize yourself...
*)