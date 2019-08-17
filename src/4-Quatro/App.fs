module Quatro.App

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<RequireQualifiedAccess>]
module Configure =
  
  open AspNetCore.DistributedCache.RavenDB
  open Data
  open Indexes
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
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
      st.Conventions.CustomizeJsonSerializer <-
        fun x -> Converters.all |> List.ofSeq |> List.iter x.Converters.Add
      st.Initialize ()
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)
    svc.AddSingleton(store)
      .AddDistributedRavenDBCache(fun opts -> opts.Store <- store)
      .AddSession(
        fun opts ->
            opts.Cookie.Name        <- ".Quatro.Session"
            opts.Cookie.IsEssential <- true)
      .AddGiraffe ()
    |> ignore
  
  let errorHandler (ex : exn) _ =
    let error = sprintf "%s - %s" (ex.GetType().Name) ex.Message
    sprintf "%s\n%s" error ex.StackTrace
    |> System.Console.WriteLine
    clearResponse >=> ServerErrors.INTERNAL_ERROR error

  let webApp : HttpHandler =
    choose [
      route "/"     >=> Handlers.home
      route "/seed" >=> Handlers.seed
      RequestErrors.NOT_FOUND "Not Found"
      ]

  let app (app : IApplicationBuilder) =
    app.UseSession()
      .UseGiraffeErrorHandler(errorHandler)
      .UseGiraffe webApp

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
