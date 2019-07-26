module Quatro.App

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<RequireQualifiedAccess>]
module Configure =
  
  open Indexes
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
  open Raven.Client.Documents
  open Raven.Client.Documents.Indexes
  
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
    svc.AddSingleton (store.Initialize ()) |> ignore
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)

  let app (app : IApplicationBuilder) =
    app.UseGiraffe (htmlString "Hello World from Giraffe")

[<EntryPoint>]
let main _ =
  use host =
    WebHostBuilder()
      .ConfigureAppConfiguration(Configure.configuration)
      .UseKestrel()
      .ConfigureServices(Configure.services)
      .Configure(System.Action<IApplicationBuilder> Configure.app)
      .Build ()
  host.Run()
  0
