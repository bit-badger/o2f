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

As with **Dos**, we need a reference to `Nancy.Session.RavenDB`; we also are going to need a package called
`TaskBuilder.fs`. Both of these are already covered by the packages we already reference in `paket.dependencies`, so we
just need to add both of these to `paket.references`, then run `paket install`.

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
is the easiest; and, since we have a `ContentType` property, we don't actually need to write the underlying CLR type to
the database. Here's the declaration, up through `WriteJson`.
*)
type IArticleContentConverter () =
  inherit JsonConverter<IArticleContent> ()

  override __.WriteJson (w : JsonWriter, v : IArticleContent, _ : JsonSerializer) =
    let writePair k (v : string) =
      w.WritePropertyName k
      w.WriteValue        v
    w.WriteStartObject ()
    writePair "ContentType" v.ContentType
    writePair "Text"        v.Text
    w.WriteEndObject ()
(**
Each `Write*` call in Json.NET writes a token; these tokens describe the shape of the JSON that will eventually be
serialized. Since we're intercepting a single property, but writing multiple values, we need to identify it as an
object. Then, we write two sets of property name and value tokens, and an end object token.

Reading it back is a bit different:
*)
  override __.ReadJson (r : JsonReader, _, _, _, _) =
    let readIgnore = r.Read >> ignore
    let typ  = (readIgnore >> r.ReadAsString) () // PropertyName -> String
    let text = (readIgnore >> r.ReadAsString) () // PropertyName -> String
    readIgnore () // EndObject
    let content : IArticleContent =
      match typ with
      | ContentType.Html -> upcast HtmlArticleContent ()
      | ContentType.Markdown -> upcast MarkdownArticleContent ()
      | x -> invalidOp (sprintf "Cannot deserialize %s into IArticleContent" x)
    content.Text <- text
    content
(**
When `ReadJson` is called, the starting object token has already been read. We read the tokens back in the order in
which we wrote them, so we'll see a property name token, then a value token. We know the property that we wrote, so we
can ignore that token; then, we know the value that we wrote is a string, so we can read the next token's value as a
string. We'll repeat that process for the actual text, and then, we need one more read to advance the reader past the
end object token. Once we have our property values, the `ContentType` tells us what type of implementation to construct,
and we can use the `Text` setter on the `IArticleContent` interface to put the text in the object.

This can seem convoluted, but hopefully the comments above help somewhat. I learned about what I've explained in this
section when I encountered _this_ problem for _this_ project. Also, most of our other `JsonConverter`s will be simply
transforming the value as it's written and read, not writing an entire object based on the data type of the property.
Even this converter, though, easily fits on one screen, and thanks to F#'s function composition, it isn't repetitive;
nearly every line does something different.

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