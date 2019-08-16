(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Owin/lib/netstandard2.0/Microsoft.AspNetCore.Owin.dll"
#r "../../../packages/Nancy/lib/netstandard2.0/Nancy.dll"
#r "../../../packages/Nancy.Session.Persistable/lib/netstandard2.0/Nancy.Session.Persistable.dll"
#r "../../../packages/Nancy.Session.RavenDB/lib/netstandard2.0/Nancy.Session.RavenDB.dll"
#r "../../../packages/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../../packages/RavenDb.Client/lib/netstandard2.0/Raven.Client.dll"

namespace Tres

open Newtonsoft.Json
open Raven.Client.Documents.Indexes

module Indexes =
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
[<AutoOpen>]
module Domain =
  [<RequireQualifiedAccess>]
  module ContentType =
    [<Literal>]
    let Html = "Html"
    [<Literal>]        
    let Markdown = "Markdown"
  type IArticleContent =
    abstract member ContentType : string with get
    abstract member Text : string with get, set
    abstract member Generate : unit -> string
  type HtmlArticleContent () =
    let mutable text = ""
    override __.ToString () = sprintf "HTML -> %s" text
    interface IArticleContent with
      member __.ContentType = ContentType.Html
      member __.Text with get () = text and set v = text <- v
      member __.Generate () = text
  type MarkdownArticleContent () =
    let mutable text = ""
    override __.ToString () = sprintf "Markdown -> %s" text
    interface IArticleContent with
      member __.ContentType = ContentType.Markdown
      member __.Text with get () = text and set v = text <- v
      member __.Generate () = text
(**
### Tres - Step 4

#### Dependencies

As with **Dos**, we need reference to `Nancy.Session.RavenDB` and `MiniGuid`, and we also are going to need a package
called `TaskBuilder.fs`. All of these are covered by the packages we already reference in `paket.dependencies`, so we
just need to add these three packages to `paket.references`, then run `paket install`.

#### Boostrapper Updates

We need to add the `Certificate` and `Password` properties to our `DataConfig` record, so that we can use them to
configure our RavenDB connection. It becomes:
*)
type DataConfig =
  { Url         : string
    Database    : string
    Certificate : string
    Password    : string
    }
(*** hide ***)
  with
    [<JsonIgnore>]
    member this.Urls = [| this.Url |]

(**
Then, we'll convert our bootstrapper changes to F#. The bootstrapper is at the top of `App.fs`. We'll need to add some
`open` statements...
*)
(*** hide ***)
open Indexes
open Nancy
open Raven.Client.Documents
open System.IO
(** *)
open Nancy.Session.Persistable
open Nancy.Session.RavenDB
open System.Security.Cryptography.X509Certificates

type TresBootstrapper () =
  inherit DefaultNancyBootstrapper ()

  let _store =
    (lazy
      (let cfg = File.ReadAllText "data-config.json" |> JsonConvert.DeserializeObject<DataConfig>
      (new DocumentStore (
        Urls        = cfg.Urls,
        Database    = cfg.Database,
        Certificate =
          match isNull cfg.Certificate || cfg.Certificate = "" with
          | true -> null
          | false -> new X509Certificate2(cfg.Certificate, cfg.Password))).Initialize ()
    )).Force ()
(**
This is F#'s version of lazy initialization. There are a lot more parentheses here, as we're creating objects and then
calling into their properties or methods. That's just a feature of how F# works. In C#,
`new DocumentStore().Initialize()` will work; in F#, though, we have to set off the creation of the document store like
`(new DocumentStore ()).Initialize ()`. _(The space before the `()` is optional, and these extra parentheses are
required whether it's there or not.)_

Now, we can chage `ConfigureApplicationContainer` and bring across `ApplicationStartup`.
*)
  override __.ConfigureApplicationContainer container =
    base.ConfigureApplicationContainer container
    container.Register<IDocumentStore> _store |> ignore
  
  override __.ApplicationStartup (container, pipelines) =
    base.ApplicationStartup(container, pipelines);
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, _store)
    PersistableSessions.Enable (pipelines, RavenDBSessionConfiguration _store)
(**
These are the same chages, just translated to F#.

#### Testing

As with **Dos**, we need to bring in the session extension method. Add `open Nancy.Session.Persistable` to the group of
`open` statements. C# only lets you define extension methods; however, F# allows you to define properties as well, so
`PersistableSession` is implemented as a _property_ on Nancy's `Request` object. Then, we translate the function:
*)
(*** hide ***)
open System
type HomeModule () as this =
  inherit NancyModule ()
  do
(** *)
    this.Get("/", fun _ ->
        let count =
          (this.Request.PersistableSession.Get<Nullable<int>> "Count"
           |> Option.ofNullable
           |> function Some x -> x | None -> 0) + 1
        this.Request.PersistableSession.["Count"] <- count
        (sprintf "You have visited this page %i times this session" count) :> obj)
(**

Since F# is designed to use `Option`s to represent a value that may or may not be present (rather than `null` checks),
but Nancy's sessions are designed to return `null` if a value isn't found, this is a bit more work. We have to specify
the full type (`Nullable<int>`), as F# doesn't support the trailing question mark syntax. Then, we use the `Option`
module's `ofNullable` function to convert a nullable value into an option. Finally, we use an inline function, which
behaves the same way as a `match` statement on the value it receives via the pipe operator.

As with **Uno** and **Dos**, if you run the site, you should be able to refresh and see the counter increase.

#### Data Seeding

The bulk of this process is simply bringing over the `HomeModule.Seed` method from **Dos**. However, before we take a
look at it, we need to take a quick side trip to discuss a concept we've seen in **Cinco** but have not explained yet.

##### Computation Expressions

In F#, a computation expression (CE) is an expression that can have one or more steps that produce a result. For each
type of result, there is a different type of CE. For example, there is a `seq` CE that generates a `sequence` (F#'s term
for `IEnumerable<T>`). Within a CE, you will find regular F# keywords such as `let`, `do`, and `match`; you will also
likely find a `return` keyword (which we don't normally use, because a function's last value is its implied return
value). You will also see keywords followed by an exclamation point - `let!`, `do!`, `match!`, and `return!`; these
keywords will have special meaning, and will directly affect whatever computation is being determined.

The `seq` CE also has a `yield` keyword, that acts like `yield return` does in C#; the `yield!` keyword yields each of
the items of an existing sequence. Below is an example `seq` CE, which contains the parts of a fake US phone number.
*)
(*** hide ***)
module Example =
  let x =
(** *)
    seq {
      yield "123"
      yield "555"
      yield "6789"
      }
(**

We could combine these using `let phone = x |> Seq.reduce (+)`.

##### The `task` CE

F# provides an `async` CE that is used to define a workflow that can execute asynchronous processes together with others
to produce a result. However, F#'s `Async` behaves a bit differently than the .NET `Task` and `Task<T>` types. F#'s
`Async` module does have functions like `Async.AwaitTask` that bring tasks into the F# async world. But, if we were to
write an `await` function, it would look something like:
*)
  let await task = task |> Async.AwaitTask |> Async.RunSynchronously
(**
This makes something like `let x = await (SomethingReturningATask())` or `let x = await <| SomethingReturningATask()`.
However, that code doesn't flow very well; we either have to wrap the function/method calls in parentheses, or use
pipeline operators to send it to await. Also, this only works for `Task<T>`; we have to write a different one to
handle the non-generic `Task`.

Enter the `task` CE, provided by `TaskBuilder.fs`. It became popular as part of the Giraffe project, as Giraffe's
sitting atop ASP.NET Core required it to deal with tasks. Within a `task` CE, `let!` awaits a generic task, `do!`
awaits a non-generic task, `match!` awaits a generic task, then pattern-matches on the result, and `return!` awaits a
generic task and returns the result. `return` returns a regular value as the CE's result. The type of the `task` CE is
exactly what the rest of the .NET environment expects.

Take a look at [the `seed` function](https://github.com/bit-badger/o2f/tree/master/src/3-Tres/HomeModule.fs#L12) to see
the `task` CE in action. `open FSharp.Control.Tasks.V2.ContextInsensitive` brings it into scope, and these tasks will
be run with `.ConfigureAwait(false)`; this keeps tasks from deadlocking from waiting on the same execution context.

One final note - the `:> obj` at the bottom of the `seed` function makes the return type `Task<obj>` instead of
`Task<string>`, which is the required signature for `NancyModule`'s `Get` method.

##### But Wait, There's More!

Go ahead and run it at this point, go to `http://localhost:5000/seed`, and compare the RavenDB documents for posts or
pages against the ones created for **Dos**. You'll notice that the content types (our `IArticleContent` fields) do not
have the `ContentType` and `Text` properties! For some reason, this is serialized differently if it's part of an F#
record type. _(This may be an edge-case bug; if I can isolate the behavior, I'll submit an issue.)_ However, this gives
us a chance for an early look at writing our own custom `JsonConverter`s. In step 3 **Quatro**, we added FSharpLu's
compact DU converter to RavenDB's configuration; this time, we'll write our own.

Just below the definition of `MarkdownArticleContent`, we'll create `IArticleContentConverter`, which will extend
`JsonConverter<IArticleContent>`. Generic converters need to implement two methods, `WriteJson` and `ReadJson`. Writing
is the easiest; and, we'll use the `ContentType` property to write a very succinct JSON object in of the form
`{ "[Html|Markdown]" : [text] }`. Here's the declaration, up through `WriteJson`.
*)
type IArticleContentConverter () =
  inherit JsonConverter<IArticleContent> ()
  
  override __.WriteJson (w : JsonWriter, v : IArticleContent, _ : JsonSerializer) =
    w.WriteStartObject ()
    w.WritePropertyName v.ContentType
    w.WriteValue v.Text
    w.WriteEndObject ()
(**
Each `Write*` call in Json.NET writes a token; these tokens describe the shape of the JSON that will eventually be
serialized. Since we're intercepting a single property, but writing an object, we write the start object token. Then,
we use `ContentType` for the property name, and `Text` for the value. We also need to write an end object token.

Reading it back is a bit different:
*)
  override __.ReadJson (r : JsonReader, _, _, _, _) =
    let typ  = r.ReadAsString () // PropertyName
    let text = r.ReadAsString () // String
    (r.Read >> ignore) () // EndObject
    let content : IArticleContent =
      match typ with
      | ContentType.Html -> upcast HtmlArticleContent ()
      | ContentType.Markdown -> upcast MarkdownArticleContent ()
      | x -> invalidOp (sprintf "Cannot deserialize %s into IArticleContent" x)
    content.Text <- text
    content
(**
When `ReadJson` is called, the starting object token has already been read. We read the tokens back in the order in
which we wrote them, so as we read through the `JsonReader`, we'll encounter our property name first, then the value
token. _(We also need one more read to advance the reader past the end object token.)_ Once we have our property values,
the `ContentType` tells us what type of implementation to construct, and we can use the `Text` setter on the
`IArticleContent` interface to put the text in the object.

> A bit of encouragement may be good here - I learned about what I've explained in this section when I encountered
> _this_ problem for _this_ project, and what you see above is the second version of it. I had written simple
> transformation `JsonConverter`s before, and the others we'll write will be like that. To me, this has been an
> illustration that no language is immune from the occasional odd behavior; but, if we apply what we already know, and
> do a bit of documentation diving, we'll often find an elegant solution. We might even no longer classify the behavior
> we encountered as odd!

We do need to revisit our RavenDB connection, though, as we need to actually plug our converter into the document store.
Here's the updated definition of `_store` from `App.fs`:
*)
(*** hide ***)
module UpdatedStore =
(** *)
  let _store =
    (lazy
     (let cfg = File.ReadAllText "data-config.json" |> JsonConvert.DeserializeObject<DataConfig>
      let store =
        new DocumentStore (
          Urls        = cfg.Urls,
          Database    = cfg.Database,
          Certificate =
            match isNull cfg.Certificate || cfg.Certificate = "" with
            | true -> null
            | false -> new X509Certificate2(cfg.Certificate, cfg.Password))
      store.Conventions.CustomizeJsonSerializer <-
        fun x -> x.Converters.Add (IArticleContentConverter ())
      store.Initialize ()
    )).Force ()
(**
At this point, use RavenDB studio to delete the existing documents, re-run the application, and re-seed the database.
You should now see `ContentType` and `Text` fields under the page/post's `Text` and `Revision` properties.

#### Conclusion

We did another C# to F# translation. We also learned about computation expressions in general, the `task` CE
specifically, and how to write a non-trivial JSON converter. Nice job - now, on to **Quatro**!

---
[Back to step 4](../step4)
*)