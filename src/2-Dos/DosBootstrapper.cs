using Dos.Data;
using Dos.Data.Indexes;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Session.Persistable;
using Nancy.Session.RavenDB;
using Nancy.TinyIoc;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Dos
{
    public class DosBootstrapper : DefaultNancyBootstrapper
    {
        private static IDocumentStore Store = new Lazy<IDocumentStore>(() =>
        {
            var cfg = JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText("data-config.json"));
            return new DocumentStore
            {
                Urls = cfg.Urls,
                Database = cfg.Database,
                Certificate = string.IsNullOrEmpty(cfg.Certificate)
                    ? null
                    : new X509Certificate2(cfg.Certificate, cfg.Password)
            }.Initialize();
        }).Value;

        public DosBootstrapper() : base() { }

        public override void Configure(Nancy.Configuration.INancyEnvironment environment)
        {
            environment.Tracing(enabled: false, displayErrorTraces: true);
        }
        protected override void ConfigureApplicationContainer(TinyIoCContainer container)
        {
            base.ConfigureApplicationContainer(container);
            container.Register(Store);
        }
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            IndexCreation.CreateIndexes(typeof(Categories_ByWebLogIdAndSlug).Assembly, Store);
            PersistableSessions.Enable(pipelines,
                new RavenDBSessionConfiguration(Store) { LogLevel = SessionLogLevel.Debug });
        }
    }
}