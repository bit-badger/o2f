﻿namespace Tres

open Indexes
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Nancy
open Nancy.Owin
open Nancy.Session.Persistable
open Nancy.Session.RavenDB
open Newtonsoft.Json
open Raven.Client.Documents
open Raven.Client.Documents.Indexes
open System.IO
open System.Security.Cryptography.X509Certificates
open Tres.Domain

type TresBootstrapper () =
  inherit DefaultNancyBootstrapper ()

  let _store =
    (lazy
     (let cfg = File.ReadAllText "data-config.json" |> JsonConvert.DeserializeObject<DataConfig>
      let store =
        new DocumentStore (
          Urls        = cfg.Urls,
          Database    = cfg.Database,
          Certificate =
            match isNull cfg.Certificate || cfg.Certificate = "" with
            | true -> null
            | false -> new X509Certificate2(cfg.Certificate, cfg.Password))
      store.Conventions.CustomizeJsonSerializer <-
        fun x -> x.Converters.Add (IArticleContentConverter ())
      store.Initialize ()
    )).Force ()

  override __.Configure environment =
    environment.Tracing (enabled = false, displayErrorTraces = true)
    
  override __.ConfigureApplicationContainer container =
    base.ConfigureApplicationContainer container
    container.Register<IDocumentStore> _store |> ignore
  
  override __.ApplicationStartup (container, pipelines) =
    base.ApplicationStartup(container, pipelines);
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, _store)
    PersistableSessions.Enable (pipelines, RavenDBSessionConfiguration _store)


type Startup() =
  member __.Configure (app : IApplicationBuilder) =
    app.UseOwin (fun x -> x.UseNancy (fun opt -> opt.Bootstrapper <- new TresBootstrapper()) |> ignore) |> ignore

module App = 
  [<EntryPoint>]
  let main argv = 
    use host = (new WebHostBuilder()).UseKestrel().UseStartup<Startup>().Build ()
    host.Run ()
    0