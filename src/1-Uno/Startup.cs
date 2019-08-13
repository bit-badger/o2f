using AspNetCore.DistributedCache.RavenDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using System.Security.Cryptography.X509Certificates;
using Uno.Data.Indexes;

namespace Uno
{
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }

        private IHostingEnvironment Env { get; set; }
        
        public Startup(IHostingEnvironment env, IConfiguration cfg)
        {
            Configuration = cfg;
            // new ConfigurationBuilder()
            //     .SetBasePath(env.ContentRootPath)
            //     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            //     .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            //     .AddEnvironmentVariables()
            //     .Build();
            Env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var cfg = Configuration.GetSection("RavenDB");
            
            var store = new DocumentStore
            {
                Urls = new[] { cfg["Url"] },
                Database = cfg["Database"],
                Certificate = cfg["Certificate"] == null
                    ? null
                    : new X509Certificate2(cfg["Certificate"], cfg["Password"])
            }.Initialize();
            IndexCreation.CreateIndexes(typeof(Categories_ByWebLogIdAndSlug).Assembly, store);
            
            services.AddSingleton(store)
                .AddDistributedRavenDBCache(options => options.Store = store)
                .AddSession(options =>
                {
                    options.Cookie.Name = ".Uno.Session";
                    options.Cookie.IsEssential = true;
                })
                .AddMvc()
                    .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        public void Configure(IApplicationBuilder app) =>
            (Env.IsDevelopment() ? app.UseDeveloperExceptionPage() : app.UseExceptionHandler("/Home/Error"))
                .UseHttpsRedirection()
                .UseStaticFiles()
                .UseSession()
                .UseMvcWithDefaultRoute();
    }
}