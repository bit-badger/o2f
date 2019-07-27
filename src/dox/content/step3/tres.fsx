(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Owin/lib/netstandard2.0/Microsoft.AspNetCore.Owin.dll"
#r "../../../packages/Nancy/lib/netstandard2.0/Nancy.dll"
#r "../../../packages/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../../packages/RavenDb.Client/lib/netstandard2.0/Raven.Client.dll"
(**
### Tres - Step 3

We'll start out, as we did with **Dos**, with adding the RavenDB dependency to `paket.references`:

    RavenDb.Client

`paket install` installs it for this project.

#### Configuring the Connection

Since **Tres** is more-or-less a C#-to-F# conversion from **Dos**, we'll use the same `data-config.json` file, in the
root of the project:

    [lang=json]
    {
      "Url": "http://localhost:8080",
      "Database": "O2F3"
    }

We'll also add it to `Tres.fsproj` to make sure it gets copied to the output; for this, though, add it to the
`ItemGroup` with the compiled items, just under `App.fs`:

    [lang=xml]
    <Content Include="data-config.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>

Now, we'll create a file `Data.fs` to hold what we had in the `Data` directory in the prior two solutions.  It should
be added to the build order after `Domain.fs` and before `HomeModule.fs`.  We'll start this file off with our
`DataConfig` implementation:
*)
namespace Tres

open Newtonsoft.Json
open System

type DataConfig =
  { Url      : string
    Database : string
    }
  with
    [<JsonIgnore>]
    member this.Urls = [| this.Url |]
(**
This should be familiar at this point; we're using a record type instead of a class, and we've added an instance member
the same way we did the static `Empty` members on our entities. JSON.Net will have no trouble deserializing to a record
type instead of a class; it actually has very good support for F# data constructs. We will tweak some of them when we
get to **Quatro**.

The `Collection.cs` static class with constants is brought over as a module (below the data configuration code). The
`RequireQualifiedAccess` attribute means that `Collection` cannot be `open`ed; this prevents us from possibly providing
an unintended version of the identifier `Post` (for example).
*)
[<RequireQualifiedAccess>]
module Collection =
  let Category = "Categories"
  let Comment  = "Comments"
  let Page     = "Pages"
  let Post     = "Posts"
  let User     = "Users"
  let WebLog   = "WebLogs"
  
  let IdFor coll (docId : Guid) = sprintf "%s/%s" coll (docId.ToString "N")

  let FromId docId =
    try
      let parts = (match isNull docId with true -> "" | false -> docId).Split '/'
      match parts.Length with
      | 2 -> parts.[0], Guid.Parse parts.[1]
      | _ -> "", Guid.Empty
    with :?FormatException -> "", Guid.Empty
(**
#### Ensuring Indexes Exist

To hold our indexes, we'll create a file `Indexes.fs`, and add it to the build process between `Domain.fs` and
`Data.fs`. This way, `Data.fs` is reserved for our data access logic, and we will be able to reference those index
types in it.

At this point, though, we encounter one of our first "seams" between the F# world and the C# world. RavenDB's
strongly-typed index creation task is designed to receive an anonymous object (the `select new { }...` construct). F#
does not have that feature, and as of this writing, index creation does not work with F#'s anonymous record type
construct (`{| Name: value; ... |}`). However, there is another way to create indexes, and we will still have them
named, and can still create them easily on application startup.

Instead of an `AbstractIndexCreationTask<'T>`, we can use at `AbstractJavaScriptIndexCreationTask` instead. This type
allows the user to provide a JavaScript mapping function that will be used to generate the index. Even better, though,
was something I stumbled across while trying to define RavenDB indexes in F#; you can also provide LINQ expressions.
RavenDB Studio allows us to view index definitions, and in viewing the indexes from **Uno**, I saw that those
definitions were text versions of the functional equivalent of our LINQ query (`xs.Select(x => new { .. })` instead of
`from x in xs select new { ... }`). So, while RavenDB does provide a mechanism to describe indexes in JavaScript (and,
if you are so inclined,
[read all about it on their docs](https://ravendb.net/docs/article-page/4.2/csharp/indexes/javascript-indexes)), we will
use it to specify the same indexes we've already designed.

Here's a look at our example index, utilizing the `AbstractJavaScriptIndexCreationTask`:
*)
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
> F# 4.7 should be bringing the `nameof` operator that C# has had for a while. When this operator arrives, we could use
> it to create this string, which would serve as an access of the field, enabling it to show up when looking for all the
> places it is used. For now, though, we'll have to remember to check these indexes if we decide to start renaming
> things.

#### Dependency Injection

We'll do the same thing we did for **Dos** - override `DefaultNancyBootstrapper` and register our connection there.
We'll do all of this in `App.fs`. We will need some additional `open` statements:

    [lang=fsharp]
    open Indexes
    open Nancy
    open Newtonsoft.Json
    open Raven.Client.Documents
    open Raven.Client.Documents.Indexes
    open System.IO

Then, we'll create our bootstrapper above the definition for `type Startup()`:
*)
(***hide***)
open Microsoft.AspNetCore.Builder
open Nancy
open Nancy.Owin
open Raven.Client.Documents
open System.IO
(***)
type TresBootstrapper () =
  inherit DefaultNancyBootstrapper ()

    override __.ConfigureApplicationContainer container =
      base.ConfigureApplicationContainer container
      let cfg = File.ReadAllText "data-config.json" |> JsonConvert.DeserializeObject<DataConfig>
      let store = new DocumentStore (Urls = cfg.Urls, Database = cfg.Database)
      container.Register<IDocumentStore> (store.Initialize ()) |> ignore
      IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)
(**
This should look very familiar, but we were able to flip some parameters to make the lines more expression-like. This
also illustrates the F# version of C#'s object initializer syntax; the line instantiating `DocumentStore` is a direct
translation of the C# version.

Now, as with **Dos**, we need to modify `Startup` (just below where we put this code) to use this new bootstrapper.
*)
  member __.Configure (app : IApplicationBuilder) =
    app.UseOwin (fun x -> x.UseNancy (fun opt -> opt.Bootstrapper <- new TresBootstrapper()) |> ignore) |> ignore
(**
At this point, once `dotnet run` displays the "listening on port 5000" message, we should be able to look at RavenDB's
`O2F3` database and indexes, just as we could for **Uno** and **Dos**.

---
[Back to Step 3](../step3)
*)