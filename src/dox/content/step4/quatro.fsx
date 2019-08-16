(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/AspNetCore.DistributedCache.RavenDB/lib/netstandard2.0/AspNetCore.DistributedCache.RavenDB.dll"
#r "../../../packages/Giraffe/lib/netstandard2.0/Giraffe.dll"
#r "../../../packages/MarkdownSharp/lib/netstandard2.0/MarkdownSharp.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Extensions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Extensions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Features/lib/netstandard2.0/Microsoft.AspNetCore.Http.Features.dll"
#r "../../../packages/Microsoft.AspNetCore.Session/lib/netstandard2.0/Microsoft.AspNetCore.Session.dll"
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

namespace Quatro

open Raven.Client.Documents.Indexes

module Indexes =
  type Categories_ByWebLogIdAndSlug () =
    inherit AbstractJavaScriptIndexCreationTask ()
    do ()

module Handlers =
(*** define: session-handler ***)
  open Giraffe
  open Microsoft.AspNetCore.Http

  let home : HttpHandler =
    fun next ctx ->
        let count =
          (ctx.Session.GetInt32 "Count"
           |> Option.ofNullable
           |> function Some x -> x | None -> 0) + 1
        ctx.Session.SetInt32 ("Count", count)
        text (sprintf "You have visited this page %i times this session" count) next ctx
(** *)
(*** hide ***)
  let seed : HttpHandler =
    fun next ctx ->
        text "" next ctx
module Domain =
  type WebLogId = WebLogId of string
  module WebLogId =
    let toString x = match x with WebLogId y -> y
  type Ticks = Ticks of int64
(**
### Quatro - Step 4

#### Dependencies

Our dependencies don't change that much for this project; we won't change it to reference
`Microsoft.AspNetCore.App`, but we'll add the RavenDB distributed cache, and we'll need to reference the session package
as well. The new references are:

    AspNetCore.DistributedCache.RavenDB
    Microsoft.AspNetCore.Session

Then, run `paket install` to register these as as part of this project.

#### The `App.fs` File

For **Uno**, we made a lot of changes to `Startup.cs`; since we initialized everything a bit more functionally, we'll
make these changes in `App.fs` instead.

First up, we'll add some `open`s:
*)
(*** hide ***)
module App =
  module Configure =

    open Giraffe
    open Indexes
    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.Configuration
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.FSharpLu.Json
    open Raven.Client.Documents
(** *)
    open AspNetCore.DistributedCache.RavenDB
    open System.Security.Cryptography.X509Certificates
    // ...
    let services (svc : IServiceCollection) =
      let config = svc.BuildServiceProvider().GetRequiredService<IConfiguration> ()
      let cfg = config.GetSection "RavenDB"
      let store =
        let st =
          new DocumentStore
           (Urls = [| cfg.["Url"] |],
            Database = cfg.["Database"],
            Certificate =
              match cfg.["Certificate"] with
              | null -> null
              | _ -> new X509Certificate2 (cfg.["Certificate"], cfg.["Password"]))
        st.Conventions.CustomizeJsonDeserializer <-
          fun x ->
              x.Converters.Add (CompactUnionJsonConverter ())
        st.Initialize ()
(**
As with our others, we're adding certificate capability to the connection, and then initializing it into a variable,
which allows us to use our initialized document store to create indexes, register it with DI, and use it to set up our
session cache.
*)
      IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)
      svc.AddSingleton(store)
        .AddDistributedRavenDBCache(fun opts -> opts.Store <- store)
        .AddSession(
          fun opts ->
              opts.Cookie.Name        <- ".Quatro.Session"
              opts.Cookie.IsEssential <- true)
        .AddGiraffe ()
      |> ignore
(**
The above is pretty much a C#-to-F# translation of the remainder of `ConfigureServices` from **Uno**, except for the
cookie name and the `.AddGiraffe ()` call. The fact that the latter wasn't on our previous Giraffe steps is technically
an error, though this is the first step where it will bite us.

#### ...then Testing

Before we tackle `ConfigureApp`, we need to actually write the handlers that we need for this step. Our handlers won't
be one-liners, but they'll still be rather simple. First, create a file `Handlers.fs`, and add it to the build order
just before `App.fs`. Make the first line `module Quatro.Handlers`, then we can write our session counter handler:
*)
(*** include: session-handler ***)
(**
While we're at it, we'll put in a stub for `seed` as well, simply returning `text "" next ctx`.

#### ...then Back to `App.fs`

Now that we have our two handlers, we need to define routes for Giraffe, so it knows at which path each one should be
executed. This is done through
[Giraffe's routing functions](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#routing). The
simplest set of functions that will get us our results is:
*)
    let webApp : HttpHandler =
      choose [
        route "/"     >=> Handlers.home
        route "/seed" >=> Handlers.seed
        RequestErrors.NOT_FOUND "Not Found"
        ]
(**
Notice the type on `webApp` - it's our familiar `HttpHandler`. The `>=>` operator is what lets us chain these
`HttpHandler`s together. Defined as we did above, we have a nice central place to view all our routes. However, because
each of them is an `HttpHandler`, we could just as easily define routers within handler modules, and attach them to a
route group. This is another case where composition gives us flexibility to structure things the way that makes the
most sense for the application.

With our router defined, we can now plug it into the application pipeline. We'll also enable sessions.
*)
    let app (app : IApplicationBuilder) =
      app.UseSession()
        .UseGiraffe webApp
(**

#### Data Seeding

The `/seed` endpoint is familiar. However, as this was the step where we implemented single-case DUs to make our domain
less capable of representing an invalid state, there are several changes that will need to be made to the function as it
was defined in **Tres**. [Review it](https://github.com/bit-badger/o2f/tree/master/src/4-Quatro/Handlers.fs#L19) to see
how we define these.

If we build and run the seed at this point, we're going to see two big problems.

1. Our single-case DUs are serialized as...
    
    [lang="json"]
    "WebLogId": {
        "WebLogId": "WebLogs/pmzIKMQtoxtuFGdkXQSBLQFRCS"
    },
    "AuthorId": {
        "UserId": "Users/DFCHSWudsgsuDMzAHUneCHAjua"
    },
    // ...
    "PublishedOn": {
        "Ticks": 637008955351836900
    },
    "UpdatedOn": {
        "Ticks": 637008955351836900
    },
    // ...
    "Tags": [
        {
            "Tag": "candidate"
        },
        {
            "Tag": "congress"
        },
        {
            "Tag": "election"
        },
        {
            "Tag": "president"
        },
        {
            "Tag": "result"
        }
    ],

This is better than `Case` and `Fields` pairs, but it's still not quite what we want; we want to be able to write a
query that says `Where(fun x -> x.WebLogId = [our-id])`, not `Where(fun x -> x.WebLogId.WebLogId = [our-id])`. Plus, the
entire reason we're using `MiniGuid`s is to save space, and this takes up more space than we're saving!

Thankfully, though, Json.NET comes to the rescue once again. We can write some really simple converters that will
take care of our single-case DUs. Create `Data.fs` and add it to the list of compiled files between `Indexes.fs` and
`Handlers.fs`. The top line should be `module Quatro.Data`, then create a `Converters` module below it; we'll put all
the converters there. The converters themselves don't have much to them; here are converters for `Ticks` and
`WebLogId`s:
*)
(*** hide ***)
module Data =
  module Converters =
    open Domain
    open Newtonsoft.Json
(** *)
    /// JSON converter for Ticks
    type TicksJsonConverter () =
      inherit JsonConverter<Ticks> ()
      override __.WriteJson(w : JsonWriter, v : Ticks, _ : JsonSerializer) =
        let (Ticks x) = v
        w.WriteValue x
      override __.ReadJson(r: JsonReader, _, _, _, _) =
        (string >> int64 >> Ticks) r.Value
    /// JSON converter for WebLogId
    type WebLogIdJsonConverter () =
      inherit JsonConverter<WebLogId> ()
      override __.WriteJson(w : JsonWriter, v : WebLogId, _ : JsonSerializer) =
        (WebLogId.toString >> w.WriteValue) v
      override __.ReadJson(r: JsonReader, _, _, _, _) =
        (string >> WebLogId) r.Value
(**
Once we get all our converters defined, we will define an `all` property on the `Converters` module. We'll also add the
FSharpLu converter to this list, so `all` will be all the `JsonConverter`s we need for our application, and we can use
them consistently throughout the application.
*)
    // add
    open Microsoft.FSharpLu.Json
    // ...
    let all : JsonConverter seq =
      seq {
        // other converters
        yield TicksJsonConverter ()
        yield WebLogIdJsonConverter ()
        yield CompactUnionJsonConverter true
        }
(**
> A note about converter order: Json.NET will pick the first converter in registration order that can convert a
> particular type it's trying to serialize or deserialize. `CompactUnionJsonConverter` converts anything that looks
> like a DU, so it needs to be last in the registration order. While it may offend some people's sense of order to
> have an alphabetical list, then a stray at the bottom, if we want it to work correctly, this is how it needs to be.

Now, we need to let RavenDB know about all these new converters. In `App.fs`, within the `services` function, we can
remove the `open` for `Microsoft.FSharpLu.Json`, add one for `Data` (our new module). The configuration of the JSON
serializers now looks like this:
      
    [lang=fsharp]
    st.Conventions.CustomizeJsonSerializer <-
      fun x -> Converters.all |> List.ofSeq |> List.iter x.Converters.Add

> We won't be using it in this application, but Giraffe also uses Json.NET to serialize objects for JSON API requests.
> Using `Converters.all` to configure both RavenDB and Giraffe will ensure that, when we are looking at a JSON
> representation of our domain items, they are consistent and as we expect them to be.


#### Conclusion

// TODO

---
[Back to Step 3](../step4)
*)