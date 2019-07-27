module Cinco.App

open Freya.Core
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

module WebApp =
  
  open Freya.Machines.Http
  
  let hello =
    freyaMachine {
      handleOk (Represent.text "Hello World from Freya")
      }

[<RequireQualifiedAccess>]
module Configure =
  let app (app : IApplicationBuilder) =
    let freyaOwin = OwinMidFunc.ofFreya WebApp.hello
    app.UseOwin (fun p -> p.Invoke freyaOwin) |> ignore
  
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

open Reader

[<EntryPoint>]
let main _ =
  liftDep getStore Data.ensureIndexes
  |> run deps
  use host =
    WebHostBuilder()
      .UseKestrel()
      .Configure(System.Action<IApplicationBuilder> Configure.app)
      .Build()
  host.Run()
  0
