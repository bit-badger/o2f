namespace Quatro

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<RequireQualifiedAccess>]
module Configure =
  let app (app : IApplicationBuilder) =
    app.UseGiraffe (htmlString "Hello World from Giraffe")
    
module App =
  [<EntryPoint>]
  let main _ =
    use host =
      WebHostBuilder()
        .UseKestrel()
        .Configure(System.Action<IApplicationBuilder> Configure.app)
        .Build ()
    host.Run()
    0
