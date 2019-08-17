module Cinco.App

open Freya.Core
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<RequireQualifiedAccess>]
module Configure =
  let app (app : IApplicationBuilder) =
    let freyaOwin = OwinMidFunc.ofFreya Handlers.webApp
    app.UseOwin (fun p -> p.Invoke freyaOwin) |> ignore
  
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
