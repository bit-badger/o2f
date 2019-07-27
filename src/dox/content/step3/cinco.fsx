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

module Indexes =
  open Raven.Client.Documents.Indexes
  open System.Collections.Generic
  type Categories_ByWebLogIdAndSlug () as this =
    inherit AbstractJavaScriptIndexCreationTask ()
    do
      this.Maps <-
        HashSet<string> [
          "docs.Categories.Select(category => new {
              WebLogId = category.WebLogId,
              Slug = category.Slug
          })"
          ]
(**
### Cinco - Step 3

As with our previous versions, we need to add `RavenDb.Client` to `paket.references`, and we'll also need
`Microsoft.FSharpLu.Json`; run `paket install` after those are added. We'll also utilize the following files from prior
projects:

- `data-config.json` from **Tres**, changing the database to `O2F5`;
- `Data.fs` from **Tres** (changes described below); and
- `Collection.fs`, `Domain.fs`, and `Indexes.fs` from **Quatro**, changing the module namespaces to `Cinco`.

The section of `Cinco.fsproj` that specifies the build order should look like this:

    [lang=xml]
    <ItemGroup>
      <Compile Include="Collection.fs" />
      <Compile Include="Domain.fs" />
      <Compile Include="Indexes.fs" />
      <Compile Include="Data.fs" />
      <Compile Include="App.fs" />
      <Content Include="data-config.json">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
      </Content>
    </ItemGroup>

### Parsing `data.config` (and More)

Up to this point, we've used JSON.Net to parse `data-config.json`. There's nothing wrong with that approach, but we'll
implement our JSON parsing a different way in this project, using a library from the same people who bring us Freya
called [Chiron](https://xyncro.tech/chiron/) (pronounced "KY-ron"). Of course, to be able to use it, we have to pull it
in as a dependency. Add `nuget Chiron` to `paket.dependencies`, add `Chiron` to `paket.references`, and run
`paket install`.

We'll utilize a discriminated union to declare the supported parameters we allow to be set. Open `Data.fs`, change the
first line to `module Cinco.Data`, and delete the `Collection` module (we included that in another file). Then, we'll
add a DU with our expected parameters.
*)
(*** hide ***)
module Data =
(** *)
  open Chiron
  open Raven.Client.Documents

  type ConfigParameter =
    | Url      of string
    | Database of string
(**
We've seen DUs like this already; however, with this definition, our `DataConfig` record now becomes dead simple:
*)
  type DataConfig = { Parameters : ConfigParameter list }
(**
We'll make a module to let us parse this using Chiron. In the module, we'll also include code to configure RavenDB's
`DocumentStore` object from the configuration.
*)
  module DataConfig =
    let fromJson json =
      match Json.parse json with
      | Object config ->
          { Parameters =
              config
              |> Map.toList
              |> List.map (fun item ->
                  match item with
                  | "Url",      String x -> Url x
                  | "Database", String x -> Database x
                  | key, value -> invalidOp <| sprintf "Unexpected RavenDB config parameter %s (value %A)" key value)
            }
      | _ -> { Parameters = [] }
(**
Before we continue, let's take a look at this; there are several new concepts here.

- In other versions, if the JSON didn't parse, we raised an exception, but that was about it. In this one, if the JSON
doesn't parse, we get a configuration that will end up making no changes to the default `DocumentStore` (which will fail
on connect, because we haven't specified any URLs). Maybe this is better, maybe not, but it demonstrates that there is a
way to handle bad JSON other than an exception.
- `Object` and `String` (and `Number`, though we don't have any) are Chiron types (cases of a DU, actually), so our
`match` statement uses the destructuring form to "unwrap" the DU's inner value.
- This version will raise an exception if we attempt to set an option that we do not recognize (something like
"Databsae" - not that anyone I know would ever type it like that...).

So, it's more code than `JsonConvert.fromJson`, but it gives us control over the deserialization. If we get an
unexpected parameter, our exception tells what that parameter is. We could also write this in such a way as to raise an
exception if the parsed JSON doesn't include a `Url` parameter. And, while the parsing of JSON is still in a black box,
how we handle each value is not.

Moving on to configuring the `DocumentStore`:
*)
    let configureStore config store  =
      config.Parameters
      |> List.fold
          (fun (stor : DocumentStore) par -> 
              match par with
              | Url url -> stor.Urls <- [| url |]; stor
              | Database db -> stor.Database <- db; stor)
          store
(**
This demonstrates a powerful concept in functional programming (not just F#), the `fold` function. All F# collection
types' modules define `fold` for them (and, though the parameter order is different, LINQ defines a similar extension
method on the `IEnumerable` types called `Aggregate` - you can even use this concept in C# and VB.NET). The concept
behind a `fold` is not terribly difficult to grasp:

- Start with a known state; in our case, when we call this, the `store` parameter will be called with
`new DocumentStore ()`.
- For each item in the collection, you run it through the function, producing a new state. Since `DocumentStore` is a
.NET class, we mutate one of its properties (depending on what parameter we're processing), and return the same object.
It doesn't have to be the same object, it just has to be the same type.
- The result is the modified state.

In F# terms, the signature is `('State -> 'T -> 'State) -> 'State -> 'T list -> 'State`. The function just below
`List.fold` has the signature of the first function; `stor` is our state, and `par` is the parameter we're processing.
We pass `store` as the second parameter, and use the `|>` operator to provide the collection. One advantage to composing
it this way is that the compiler can determine the type of `'T`, so we do not have to specify it. We could include
`config.Parameters` as the last parameter to `List.fold`, but we'd have to define the type for `par`, as the compiler
could not infer it.

Why would you want to write this in this way? Let's remember what `App.fs` looked like in **Quatro**:

    [lang=fsharp]
    let config = svc.BuildServiceProvider().GetRequiredService<IConfiguration> ()
    let cfg = config.GetSection "RavenDB"
    let store = new DocumentStore (Urls = [| cfg.["Url"] |], Database = cfg.["Database"])
    store.Conventions.CustomizeJsonDeserializer <-
      fun x ->
          x.Converters.Add (CompactUnionJsonConverter ())
    svc.AddSingleton (store.Initialize ()) |> ignore

While we won't be adding it to a DI container, the code from **Quatro** is procedural code; build the provider, get the
section, create the store, add the serializer - it's a step-by-step how-to guide for getting from zero to an initialized
connection. Conversely, `configureStore` is a description of the transformation that will be applied to a new store, one
of the hallmarks of functional thinking. Within functional programming, a "pure" function is one that has no side
effects; every time it is called with the same input, it produces the same return value, and does not rely on nor change
anything else. While some may not consider `configureStore` a pure function due to its use of mutation, from a logic
standpoint it is, as it will always make the same changes to whatever `DocumentStore` is passed. It's an isolated
transformation.
*)
(*** define: index-creation ***)
  open Indexes

  let ensureIndexes store =
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)
(**
Speaking of thinking functionally - time to replace our DI container!
*)
(*** define: readerm-definition ***)
type ReaderM<'d, 'out> = 'd -> 'out
(*** hide ***)
module Reader =
(** *)
(*** define: run ***)
  let run dep (rm : ReaderM<_,_>) = rm dep
(** *)
(*** define: lift-dep ***)
  let liftDep (proj : 'd2 -> 'd1) (rm : ReaderM<'d1, 'output>) : ReaderM<'d2, 'output> = proj >> rm
(** *)
(*** hide ***)
open Reader
(** *)
(**
#### Dependency Injection with Functional Style

One of the concepts that dependency injection is said to implement is "inversion of control;" rather than an object
compiling and linking a dependency at compile time, it compiles against an interface, and the concrete implementation
is provided at runtime. (This is a bit of an oversimplification, but it's the basic gist.) If you've ever done
non-DI/non-IoC work, and learned DI, you've adjusted your thinking from "what do I need" to "what will I need". In the
functional world, this is done through a concept called the **`Reader` monad**. The basic concept is as follows:

- We have a set of dependencies that we establish and set up in our code.
- We a process with a dependency that we want to be injected (in our case, our `IDocumentStore` is one such dependency).
- We construct a function that requires this dependency, and returns the result we seek. Though we won't see it in this
step, it's easy to imagine a function that requires an `IDocumentStore` and returns a `Post`.
- We create a function that, given our set of dependencies, will extract the one we need for this process.
- We run our dependencies through the extraction function, to the dependent function, which takes the dependency and
returns the result.

Confused yet? Me too - let's look at code instead. Let's create `Dependencies.fs` and add it to the build order above
`Domain.fs`. This write-up won't expound on every line in this file, but we'll hit the highlights to see how all
this comes together. `ReaderM` is a generic class, where the first type is the dependency we need, and the second type
is the type of our result.

After that (which will come back to in a bit), we'll create our dependencies, and a function to extract an
`IDocumentStore` from it.
*)
open Raven.Client.Documents

type IDependencies =
  abstract Store : IDocumentStore

[<AutoOpen>]
module DependencyExtraction =
  
  let getStore (deps : IDependencies) = deps.Store
(**
Our `IDependencies` are pretty lean right now, but that's OK; we'll flesh it out in future steps. We also wrote a
dead-easy function to get the store; the signature is literally `IDependencies -> IDocumentStore`. No `ReaderM` in
sight - yet!

Now that we have a dependency "set" (of one), we need to go to `App.fs` and make sure we actually have a concrete
instance of this for runtime. Change `namespace Cinco` to `module Cinco.App`, and add this just before main function:
*)
(*** hide ***)
module App =
(** *)
  open Data
  open Microsoft.FSharpLu.Json
  open Raven.Client.Documents
  open System.IO

  let cfg = (File.ReadAllText >> DataConfig.fromJson) "data-config.json"
  let deps = {
    new IDependencies with
      member __.Store
        with get () =
          let store = lazy (
            let stor = DataConfig.configureStore cfg (new DocumentStore ())
            stor.Conventions.CustomizeJsonDeserializer <-
              fun x ->
                  x.Converters.Add (CompactUnionJsonConverter ())
            stor.Initialize ()
            )
          store.Force()
      }
(**
Here, we're using `lazy` to do this only once and only-on-demand, then we turn around and demand it. If you're thinking
this sounds a lot like a singleton - your thinking is superb! That's exactly what we're doing here. We're also using
F#'s inline interface declaration to create an implementation without creating a concrete class in which it is held. If
you hover over `deps` above, you'll see that its type is `IDependencies`; even though we're using a traditional .NET
interface, we won't have to cast it to its interface type to use it, as it's an anonymous concrete implementation.

Maybe being our own IoC container isn't so bad! There is one piece missing, though, compared to our other
implementations; we aren't yet making any calls to RavenDB to make sure our indexes exist. Let's add that as a function
within `Data.fs`, at the bottom of the file:
*)
(*** include: index-creation ***)
(**
Now, let's take a stab at actually pulling our store out of our dependencies, so we can use it to make sure our indexes
exist. At the top of `main`:
*)
(*** hide ***)
  let main _ =
(** *)
    let checkIndexes store = Data.ensureIndexes store
    let start = liftDep getStore checkIndexes
    start |> run deps
(*** define: better-init ***)
    liftDep getStore Data.ensureIndexes
    |> run deps
(*** hide ***)
    0
(**
If we're letting the types be our guide, how are we doing with these? `checkIndexes` has the signature
`IDocumentStore -> unit`, `start` has the signature `ReaderM<IDependencies, unit>`, and the third line is simply `unit`
(which we can tell because the compiler isn't telling us we need to ignore the value or assign it). And, were we to run
it, it would work, but... it's not really composable. Do we really want to have to define two extra variables every time
we do something that requires a dependency? Of course not.

Notice that the signature for `checkIndexes` is the same as `Data.ensureIndexes`; `checkIndexes` is redundant. And,
there's no need to establish a variable for `start` either; we can just pipe our entire expression into `run deps`.
*)
(*** include: better-init ***)
(**
It works! We set up our dependencies, we composed a function using a dependency, and we used a `Reader` monad to make it
all work.  But, how did it work?  Given what we just learned above, let's look at the steps; we're coders, not
magicians.

First up, `liftDeps`.
*)
(*** include: lift-dep ***)
(**
The `proj` parameter is defined as a function that takes one value and returns another one. The `rm` parameter is a
`Reader` monad that takes the **return** value of `proj`, and returns a `Reader` monad that takes the **parameter**
value of `proj` and returns an output type. We passed `getStore` as the `proj` parameter, and its signature is
`IDependencies -> IDocumentStore`; the second parameter was a function with the signature `IDocumentStore -> unit`.
Where does this turn into a `ReaderM`? Why, the definition, of course!
*)
(*** include: readerm-definition ***)
(**
Remember the time we spent discussing the `>>` operator? `ReaderM` is an alias for a one-parameter function, and
`liftDep` uses it to compose one function with another, returning a one-parameter function. In concrete types for
`liftDep`'s generics in our example, using `getStore` for `proj` means that `'d1` is `IDocumentStore` and `'d2` is
`IDependencies`. (Note that `proj` is `'d2 -> 'd1`; that's why its parameters are reversed from `getStore`'s.) When we
passed `Data.ensureIndexes` as `rm`, its `'d1` is `IDocumentStore`, and `'output` is `unit`. When these two functions
are composed, we end up with a function that requires `IDependencies` (`'d2`) and returns `unit` (`'output`); this
matches the definition of `liftDep`'s output type, as `ReaderM<'d2, 'output>` is an alias for `'d2 -> 'output`.

If your head is spinning, get it stabilized, then read through that again. It can be quite complex - until it clicks.
Then, it's like a light bulb goes off above your head. "Oh, it's called a **reader** monad because it shows us how to
**read** the part we need from an object!" That's exactly what it does. We need a document store, and this other object
has it; `ReaderM` lets us specify how to "read" (obtain) that dependency. We are free to write as many
`IDocumentStore`-requiring functions as we want, and we'll be able to use `liftDep` and `getStore` to change those into
`IDependencies`-requiring functions that return the same thing.

Then, we can run them! `run` is defined as:
*)
(*** include: run ***)
(**
This is way easier than what we've seen up to this point. It takes an object and a `ReaderM`, and applies the object to
the first parameter of the monad. By `|>`ing the `ReaderM<IDependencies, unit>` to it, and providing our `IDependencies`
instance, we receive the result; the reader has successfully encapsulated all the functions below it. From this point
on, we'll just make sure our types are correct, and we'll be able to utilize not only an `IDocumentStore` for data
manipulation, but any other dependencies we may need to define.

Take a deep breath. Step 3 is done, and not only does it work, we understand why it works.   

---
[Back to Step 3](../step3)
*)