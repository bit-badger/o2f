namespace Cinco

open Freya.Core
open Freya.Machines.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

module WebApp =
  
  let hello =
    freyaMachine {
      handleOk (Represent.text "Hello World from Freya")
      }

[<RequireQualifiedAccess>]
module Configure =
  let app (app : IApplicationBuilder) =
    let freyaOwin = OwinMidFunc.ofFreya WebApp.hello
    app.UseOwin (fun p -> p.Invoke freyaOwin) |> ignore
  
module App =
  [<EntryPoint>]
  let main _ =
    use host =
      WebHostBuilder()
        .UseKestrel()
        .Configure(System.Action<IApplicationBuilder> Configure.app)
        .Build()
    host.Run()
    0
