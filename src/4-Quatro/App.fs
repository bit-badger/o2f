module Quatro.App

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<RequireQualifiedAccess>]
module Configure =
  
  open AspNetCore.DistributedCache.RavenDB
  open Indexes
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
  open Microsoft.FSharpLu.Json
  open Raven.Client.Documents
  open Raven.Client.Documents.Indexes
  open System.Security.Cryptography.X509Certificates
  
  let configuration (ctx : WebHostBuilderContext) (cfg : IConfigurationBuilder) =
    cfg.SetBasePath(ctx.HostingEnvironment.ContentRootPath)
      .AddJsonFile("appsettings.json", optional = true, reloadOnChange = true)
      .AddJsonFile(sprintf "appsettings.%s.json" ctx.HostingEnvironment.EnvironmentName, optional = true)
      .AddEnvironmentVariables ()
    |> ignore
        
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
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)
    svc.AddSingleton(store)
      .AddDistributedRavenDBCache(fun opts -> opts.Store <- store)
      .AddSession
       (fun opts ->
            opts.Cookie.Name        <- ".Quatro.Session"
            opts.Cookie.IsEssential <- true)
    |> ignore

  let app (app : IApplicationBuilder) =
    app.UseSession()
      .UseGiraffe (htmlString "Hello World from Giraffe")

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
