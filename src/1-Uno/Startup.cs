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
            // var cfg = Configuration.GetSection("RavenDB");
            
            // var store = new DocumentStore
            // {
            //     Urls = new[] { cfg["Url"] },
            //     Database = cfg["Database"],
            //     Certificate = cfg["Certificate"] == null
            //         ? null
            //         : new X509Certificate2(cfg["Certificate"], cfg["Password"])
            // }.Initialize();
            // IndexCreation.CreateIndexes(typeof(Categories_ByWebLogIdAndSlug).Assembly, store);
            
            // services.AddSingleton(store)
            //     .AddDistributedRavenDBCache(options => options.Store = store);

            services.AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            // services.AddSession(options =>
            // {
            //     options.Cookie.Name = ".Uno.Session";
            //     options.Cookie.IsEssential = true;
            // });
        }

        public void Configure(IApplicationBuilder app)
        {
            if (Env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            // app.Use(async (context, next) =>
            // {
            //     System.Console.WriteLine("After static files, before session");
            //     await next.Invoke();
            //     System.Console.WriteLine("coming back - After session, before static files");
            // });
            // app.UseSession();
            // app.Use(async (context, next) =>
            // {
            //     System.Console.WriteLine("After session, before MVC");
            //     await next.Invoke();
            //     System.Console.WriteLine("coming back - After MVC, before session");
            // });
            app.UseMvcWithDefaultRoute();
            app.Use(async (context, next) =>
            {
                System.Console.WriteLine("After MVC");
                await next.Invoke();
                System.Console.WriteLine("coming back - Before MVC");
            });
        }
    }
}