using AspNetCore.DistributedCache.RavenDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;
using System.Linq;
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
            // app.UseSession();
            app.Use(async (context, next) =>
            {
                var p = context.RequestServices.GetRequiredService(typeof(IActionDescriptorCollectionProvider))
                    as IActionDescriptorCollectionProvider;

                System.Console.WriteLine("Here come the routes");
                foreach (var x in p.ActionDescriptors.Items)
                {
                    System.Console.WriteLine("Action = {0} | Controller = {1} | Name = {2} | Template = {3} | Constraint = {4}",
                        x.RouteValues["Action"],  x.RouteValues["Controller"],  x.AttributeRouteInfo?.Name,
                        x.AttributeRouteInfo?.Template,
                        x.ActionConstraints == null ? "" : JsonConvert.SerializeObject(x.ActionConstraints));
                }
                await next.Invoke();
            });
            app.UseMvc(routes =>
            {
                routes.MapRoute(name: "default", template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}