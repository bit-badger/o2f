namespace Quatro

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Logging
open System.IO

module Handlers =
  let hello : HttpHandler =
    htmlString "Hello world from Giraffe"

[<RequireQualifiedAccess>]
module Configure =
  let errorHandler (ex : exn) (logger : ILogger) =
    logger.LogError(EventId(), ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message
    
module App =
  [<EntryPoint>]
  let main argv =
    let contentRoot = Directory.GetCurrentDirectory ()
    WebHostBuilder()
      .UseContentRoot(contentRoot)
      .ConfigureAppConfiguration(Configure.configuration)
      .UseKestrel()
      .UseWebRoot(Path.Combine (contentRoot, "wwwroot"))
      .ConfigureServices(Configure.services)
      .Configure(System.Action<IApplicationBuilder> Configure.app)
      .Build()
      .Run ()
      0 // return an integer exit code
