(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Giraffe/lib/netstandard2.0/Giraffe.dll"
#r "../../../packages/MarkdownSharp/lib/netstandard2.0/MarkdownSharp.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.Extensions.Configuration/lib/netstandard2.0/Microsoft.Extensions.Configuration.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.Abstractions/lib/netstandard2.0/Microsoft.Extensions.Configuration.Abstractions.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.EnvironmentVariables/lib/netstandard2.0/Microsoft.Extensions.Configuration.EnvironmentVariables.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.FileExtensions/lib/netstandard2.0/Microsoft.Extensions.Configuration.FileExtensions.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.Json/lib/netstandard2.0/Microsoft.Extensions.Configuration.Json.dll"
#r "../../../packages/Microsoft.Extensions.DependencyInjection/lib/netstandard2.0/Microsoft.Extensions.DependencyInjection.dll"
#r "../../../packages/Microsoft.Extensions.DependencyInjection.Abstractions/lib/netstandard2.0/Microsoft.Extensions.DependencyInjection.Abstractions.dll"
#r "../../../packages/Microsoft.Extensions.FileProviders.Abstractions/lib/netstandard2.0/Microsoft.Extensions.FileProviders.Abstractions.dll"
#r "../../../packages/Microsoft.FSharpLu.Json/lib/netstandard2.0/Microsoft.FSharpLu.Json.dll"
#r "../../../packages/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../../packages/RavenDb.Client/lib/netstandard2.0/Raven.Client.dll"
(**
### Quatro - Step 3

Once again, we need to add the RavenDB dependency to `paket.references`; we'll also need to add some ASP.NET Core
references as well. The new references are:

    Microsoft.Extensions.Configuration.FileExtensions
    Microsoft.Extensions.Configuration.Json
    Microsoft.Extensions.Options.ConfigurationExtensions
    RavenDb.Client

The, run `paket install` to register these as as part of this project.

#### Configuring the Connection and Dependency Injection

As we're back in ASP.NET Core land, we'll do these together. We'll return to `appsettings.json` for our configuration;
create that file with these values.

    [lang=json]
    {
      "RavenDB": {
        "Url": "http://localhost:8080",
        "Database": "O2F4"
      }
    }

We'll also need to make sure the file is copied to the build directory, so add the following to `Quatro.fsproj`, inside
the list of compiled files:

    [lang=xml]
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>

In [step 2](../step1/quatro-cinco.html), we mentioned that we are going to change most of our namespace declarations to
modules instead. In `App.fs`, let's change `namespace Quatro` to `module Quatro.App`, remove the `module App`
declaration near the bottom of the file, and move `let main` out to column 1. After that, we will convert the
initialization and DI logic to the function-based initialization we used in step 1. When the functions get pulled into
the `WebHostBuilder`, their shapes may be a bit different than the `Startup` class-based ones.

Here is what our new `Configure` module looks like:

*)
(*** hide ***)
namespace Quatro
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
(** *)
[<RequireQualifiedAccess>]
module Configure =
  
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
(*** define: open-fsharplu ***)
  open Microsoft.FSharpLu.Json
(** *)
  open Raven.Client.Documents
  
  let configuration (ctx : WebHostBuilderContext) (cfg : IConfigurationBuilder) =
    cfg.SetBasePath(ctx.HostingEnvironment.ContentRootPath)
      .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
      .AddJsonFile(sprintf "appsettings.%s.json" ctx.HostingEnvironment.EnvironmentName, optional = true)
      .AddEnvironmentVariables ()
    |> ignore
        
  let services (svc : IServiceCollection) =
    let config = svc.BuildServiceProvider().GetRequiredService<IConfiguration> ()
    let cfg = config.GetSection "RavenDB"
    let store = new DocumentStore (Urls = [| cfg.["Url"] |], Database = cfg.["Database"])
(*** define: add-fsharplu ***)
    store.Conventions.CustomizeJsonDeserializer <-
      fun x ->
          x.Converters.Add (CompactUnionJsonConverter ())
(** *)
    svc.AddSingleton (store.Initialize ()) |> ignore

  let app (app : IApplicationBuilder) =
    app.UseGiraffe (htmlString "Hello World from Giraffe")
(**
Then, within our `main`, we'll call these new functions; our `host` definition now looks like:
    
    [lang=fsharp]
    use host =
      WebHostBuilder()
        .ConfigureAppConfiguration(Configure.configuration)
        .UseKestrel()
        .ConfigureServices(Configure.services)
        .Configure(System.Action<IApplicationBuilder> Configure.app)
        .Build ()

Notice that we've moved our `open` statements for types that we do not need outside of the `Configure` module to appear
inside it. This is a way that F# can help isolate namespace conflicts. If you have ever worked on an application that
decided to name a type using a common term (`Table` comes to my mind), the few namespaces you open, the less chance you
have of running into a competing definition of the same type.

#### Ensuring Indexes Exist

We'll bring across `Indexes.fs` from **Tres**, changing it to `Quatro`, and adding it to the compile order between
`Domain.fs` and `App.fs`. Then, we can go back to our `Configure.services` function, and add the `IndexCreation` call
to create our indexes:

    [lang=fsharp]
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)

(We need to open `Raven.Client.Documents.Indexes` and `Indexes` (our module) for this call to work.)

#### Revising the Data Model

At this point, we need to change some things in our data model, before they bite us later on in the project. By
convention, when RavenDB persists POCOs, it looks for an `Id` field of type `string`, and if it does not find one, it
generates one within the document itself. _(You can also define it as `Guid` if you want, but that's not quite what
we're doing with our keys.)_ I did a lot of testing on various ways around not having to define identifiers with
`Id : string`, but none of my tricks worked.

This will be another "seam" between F# representations and the .NET environment in general. However, we wouldn't be able
to get a single-case DU from an HTTP request either, so this gives us an opportunity to learn another common F# pattern.
When we [defined the data model originally](../step2/quatro-cinco.html), when we replaced `IArticleContent` with a
multi-case DU, I added a `Generate` method to the DU's type, but mentioned that we may end up changing that. Let's do
that now, as we'll use a similar technique to develop a repeatable way to deal with these seams.

The "common pattern" is to use a type definition for the type itself, then define a module with the same name as the
type to hold the behavior associated with that type. The module will be compiled with the word `Module` appended to the
name of the class; this used to require an attribute, but was such a common pattern, it became part of the way the
language works.

Here's our new `ArticleContent` definition:
*)
(*** hide ***)
[<RequireQualifiedAccess>]
module Collection =
  open System
  let Page     = "Pages"
  let idFor coll (docId : Guid) = sprintf "%s/%s" coll (docId.ToString "N")
  let fromId docId =
    try
      let parts = (match isNull docId with true -> "" | false -> docId).Split '/'
      match parts.Length with
      | 2 -> parts.[0], Guid.Parse parts.[1]
      | _ -> "", Guid.Empty
    with :?FormatException -> "", Guid.Empty

module Domain =
  open System
(** *)
  type ArticleContent =
    | Html     of string
    | Markdown of string

  module ArticleContent =
    let generate content =
      match content with Html x -> x | Markdown y -> MarkdownSharp.Markdown().Transform y
(**
That's the pattern we'll use for our identifiers. To begin, create a file named `Collection.fs`, and bring over the
`Collection` module from **Tres**. There, we did not need it for our domain, but our implementations of the modules for
our Id types will rely on it, so it needs to appear in the compilation order ahead of `Domain.fs`. In `Quatro.fsproj`,
that's where it will go - just before `Domain.fs`. We'll also change `IdFor` to `idFor` and `FromId` to `fromId`.
([the result](https://github.com/bit-badger/o2f/tree/step-3/src/4-Quatro/Collection.fs))

As we've used the `Page` type for our examples thus far, we'll continue to use it for our example here. Our previous
definition of `PageId` was `type PageId = PageId of string`. As we think through the various edges and seams of the
project, there are four different transformations we'll need:

- Converting a string PageId to a `PageId`; this is what we'll use if the web log has a static page defined as its home
page.
- Converting a `PageId` to just the string `Guid` representation; this is what we'll use to define links to edit pages.
- Converting a just a string `Guid` to a `PageId`; this is what we'll use to grab the Id from a URL, and apply it to the
proper collection. _(We are fine if this bombs when given a non-GUID string; make invalid states unrepresentable...)_
- Converting a `PageId` to a string; this is what we'll use to set the `Page.Id` field.
- Creating a `PageId` from a `Guid`; this will be used when the user creates a new page.

The first scenario is how the constructor works; `PageId Pages/[guid-string]` is what we want there. The other four
requirements are implemented in the `PageId` module:
*)
  type PageId = PageId of string
  module PageId =
    let create = Collection.idFor Collection.Page >> PageId
    let ofString (stringGuid : string) = create (Guid.Parse stringGuid)
    let toString x = match x with PageId y -> y
    let asGuidString x = ((toString >> Collection.fromId >> snd) x).ToString "N"
(**
Thanks to the two functions we defined in the `Collection` module, we can use composition and currying a good bit here.
We've seen the `|>` operator before, the operator that sends the output of the function before it to the function after
it; the `>>` operator does a similar thing, but at the function definition level.

`create` demonstrates this; even though we did not specify any parameters on the function, its signature is
`Guid -> PageId`. When we combine functions with `>>`, its expected input will be the final input from the first
function; `Collection.idFor`'s signature is `string -> Guid -> string`, but we have provided the first string, which (by
the rules of currying) makes the statement before the `>>` have the signature `Guid -> string`. When we combine via
`>>`, the output of the first function will be applied as the last parameter of the next function. As `PageId` is a
single-case DU, its constructor's signature is `string -> PageId`. Since the return value of the first part is `string`,
and the expected input for `PageId` is string, we can combine them together. When we do, we get a function that has the
signature `Guid -> PageId`.

> Think of the difference between `|>` and `>>` as the difference between execution and definition. We just as well
> could have defined `create` as `let create x = Collection.idFor Collection.Page x |> PageId`, which implies that the
> function will execute first, then send its output to the next one; in our definition above, we describe a chain of
> functions that create a new function that just has one input and one output. Execution-wise, these are equivalent; in
> fact, `let create x = PageId (Collection.IdFor Collection.Page x)` is also equivalent.
>
> One other note - sometimes, when composing via `>>`, the compiler may complain about a "value restriction." This means
> that, in the course of execution, the thing that appears to be a function will actually be executed once, and its
> result remembered, rather than executing each time. There is a good "under the hood" reason for this, but it can seem
> to be a strange quirk the first time you encounter it. To get around it, just specify the parameter and add it to the
> function body where it's needed.

Remember, "let the types be your guide;" if you're still unclear about how those four definitions work, hover over each
piece of them. You'll see the signatures for each of the functions that we chained together, and how the output of one
is the final input parameter of the next. `snd` may be new; F# provides `fst` and `snd` to get the first and second
values in a tuple; since `Collection.fromId` returns a `string * Guid` tuple, `snd` gives us the `Guid` part.

We need to change the definition of `Page` to use `string` for the Id now.

    [lang=fsharp]
    [<CLIMutable; NoComparison; NoEquality>]
    type Page =
      { Id             : string
        // ...
        }
    with
      static member Empty = 
        { Id             = ""
          /// ...
          }

We will write similar modules for all of the Id types. This may seem like boilerplate code that we thought F# helped us
avoid; and, from one perspective, it is. However, even though it's much more code, it defines how we move from the
primitive-value world into the strongly-typed environment of our application. And, while the presence of a `PageId`
module doesn't keep us from writing `let pg = { Page.empty with Id = "abc123" }`, it does provide an easy way for us to
**not** do that. Just as we developed the discipline of adding a new file to the project file when we created it, we
will develop the discipline of creating strongly-typed Ids in our request handlers the first time we address them, and
only ever setting an Id field using one of these functions, ideally deferring this to the point where it's stored in
RavenDB. In effect, we'll push all the primitives out to the edges of the application.

We won't need these for the other single-case DUs; however, there is another tweak we'll need to make for them.

#### Single-Case DUs to JSON

I mentioned on a prior page that JSON.NET has great F# support. While this is true, it does generate some verbose output
for some types, and discriminated unions are one of those. As an example, if we have a variable named `x` defined as a
`string option` that has the value `abc123`, this will be serialized as...

    [lang=json]
    {
      "x":
        "Case": "Some",
        "Fields": [
          "Value": "abc123"
        ]
    }

There are times where this is exactly what we want. Imagine we had a DU case of type `string * int * string`; this
structure is a great way to represent it in JSON. For single-case DUs, though _(which `Option<'T>` is)_, this is going
to make it very difficult to write a query based on the value of `x`. In these cases, what we would prefer to see is
something like...

    [lang=json]
    {
      "x" : "abc123"
    }

...or, in cases where `x` is `None`...

    [lang=json]
    {
      "x": null
    }

... (or even have `x` excluded from the output).

There is a package called Microsoft.FSharpLu.Json that provides a JSON.NET converter that handles these cases; and,
since RavenDB uses JSON.NET to serialize the documents, all we have to do is tell it to use it. This will be a new
reference overall, so we'll need to add `nuget Microsoft.FSharpLu.Json` to `paket.dependencies`, and then add
`Microsoft.FSharpLu.Json` to `paket.references` in **Quatro**. (`paket install` as usual.)

Then, in `App.fs`, we'll need to open the namespace (within the `Configure` module):
*)
(*** include open-fsharplu **)
(**
...and add the following just above the call to `svc.AddSingleton`:
*)
(*** include add-fsharplu ***)
(**
This will serialize the single-case DUs as we described above, including our Id fields that aren't the actual Id of a
document (those that point to other documents; our foreign keys, in relational terms). Additionally, our multi-case DUs
that have no associated types will be serialized as strings, so statuses and levels will look just as though we were
still using a defined set of magic strings.

As we move along, we may need to write other `JsonConverter`s; if we do, we'll just need to add them to the function
above. One important thing to remember is that JSON.NET will use the first matching converter it finds; so, if we write
a converter for a DU, it will need to go above the `CompactUnionJsonConverter` or it will be used instead.

Congratulations - **Quatro** should be ready to go! Ensure you've created an O2F4 database in RavenDB, then `dotnet run`
this project and ensure that the store is initialized properly, and all indexes are created the way they were for our
previous versions. And, know that we'll carry every bit of this hard work forward to **[Cinco](./cinco.html)**;
replacing DI will be our learning activity there.

---
[Back to Step 3](../step3)
*)