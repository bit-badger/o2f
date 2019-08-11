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
        Urls = cfg.Urls,
        Database = cfg.Database,
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
the items of an existing sequence. Let's look at an example `seq` CE, which we'll use to create the parts of a fake US
phone number.
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

#### Conclusion

We did another C# to F# translation, and we learned about computation expressions and the `task` CE. Nice job - now, on
to **Quatro**!

---
[Back to step 4](../step4)
*)