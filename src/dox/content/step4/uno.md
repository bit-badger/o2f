### Uno - Step 4

#### Dependencies

If you remember all the way back to [step 1](../step1/uno.html), we only pulled in the ASP.NET Core packages we needed to do our "Hello World" app. At this point, though, we'll go ahead and bring in the entire `Microsoft.AspNetCore.App` meta-package, which will install a whole lot more, including the Razor templating engine.

In `paket.dependencies`, in the root of the solution folder, replace the following...

    nuget Microsoft.AspNetCore.Owin
    nuget Microsoft.AspNetCore.Server.Kestrel
    nuget Microsoft.Extensions.Configuration.FileExtensions
    nuget Microsoft.Extensions.Configuration.Json
    nuget Microsoft.Extensions.Options.ConfigurationExtensions

...with just one line:

    nuget Microsoft.AspNetCore.App

In addition, add `nuget AspNetCore.DistributedCache.RavenDB`. Then, make the same changes in `paket.references` for Uno, then run `paket install`.

#### The `Startup.cs` File

To begin, we'll actually simply the file; the code that we put in the constructor is actually [the way ASP.NET Core 2.2 works out-of-the-box](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-2.2#default-configuration). Instead, we can make one tweak to our private property, and have it injected into the constructor. We'll go from...

    [lang=csharp]
    public static IConfigurationRoot Configuration { get; private set; }
        
    public Startup(IHostingEnvironment env)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
            .AddEnvironmentVariables();
        Configuration = builder.Build();
    }

...to...

    [lang=csharp]
    public static IConfiguration Configuration { get; private set; }
    
    public Startup(IConfiguration cfg)
    {
        Configuration = cfg;
    }

Next, we'll turn our attention to `ConfigureServices`. Here, we'll configure our session provider, as well as bring in the MVC setup. We're also going to adapt our RavenDB connection. If we configure a server that listens on anything other than 127.0.0.1 (localhost), RavenDB insists on using a client certificate to ensure a secure connection between client and server. Our current configuration does not support it; when we're done, we'll support loading a `.pfx` file from a configured path.

Here is what `ConfigureServices` will look like:

    [lang=csharp]
    // new usings
    using AspNetCore.DistributedCache.RavenDB;
    using Microsoft.AspNetCore.Mvc;
    using System.Security.Cryptography.X509Certificates;

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
            
        _ = services.AddSingleton(store)
            .AddDistributedRavenDBCache(options => options.Store = store)
            .AddSession()
            .AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
    }

We've added the certificate check to the store, and instead of registering the result of `.Initialize()`, we're making that the value of `store`. This lets us use the initialized store in index creation, as well as in setting up the distributed cache. The chain on `services` is also new. Most calls to `.Add*` methods return the modified service collection, so we can chain several calls together instead of writing `services.Add...` over and over. The exception to this is our last call, which is why it's last.

Finally, let's turn our attention to the `Configure` method. Just like we ended up with a chain of calls for services, we're going to have the same thing on our application builder. In fact, it ends up being just one chain, so we can still use the expression-bodied member syntax!

    [lang=csharp]
    public void Configure(IApplicationBuilder app, IHostingEnvironment env) =>
        _ = (env.IsDevelopment()
                ? app.UseDeveloperExceptionPage()
                : app.UseExceptionHandler("/Home/Error"))
            .UseHttpsRedirection()
            .UseStaticFiles()
            .UseSession()
            .UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

The first expression uses the developer exception page if we're running in development; this gives us as developers a lot of information, stack traces, and such when an error occurs. In production mode, we don't want that, though, so we'll use the exception handler `/Home/Error`. (We'll write that below.) `.UseHttpsRedirection` means that we'll be redirected from HTTP to HTTPS (and, in our default configuration, from port 5000 to 5001) automatically. `.UseStaticFiles` lets us serve static files out of our `wwwroot` directory. `.UseSession` is paired with `.AddSession` from above to actually implement it into the pipeline; by putting it after the static file middleware, static files will not require a session (which is what we want). Finally, `.UseMvc` sets up our MVC route template.

#### Controllers, etc.

If you look at the last code sample above, you can see two controller actions on the `Home` controller; `/Error` for the exception handler, and `/Index` for the default action. By convention, ASP.MVC Core expects controllers to be in the `Controllers` directory and namespace, to be named `*Controller` (where, in this case, `*` is `Home`), and to inherit from the `Controller` base class (from `Microsoft.AspNetCore.Mvc`).

Create a `Controllers` directory, then create a `HomeController.cs` file. We'll use the `Content` return to create a skeleton of this file.

    [lang=csharp]
    using Microsoft.AspNetCore.Mvc;
    using System.Diagnostics;

    namespace Uno.Controllers
    {
        public class HomeController : Controller
        {
            public IActionResult Index() =>
                Content("The index");

            [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
            public IActionResult Error() =>
                Content(string.Format("Error {0}, Request ID {1}", HttpContext.Response.StatusCode,
                    Activity.Current?.Id ?? HttpContext.TraceIdentifier));

        }
    }

It's not much, but it doesn't require views, which we're not writing for a few steps. Speaking of views, though, there is some framework we can set up for them. Create a `Views` folder in the root of the application, then navigate there using a command prompt or shell. Execute the following two commands in that directory:

    dotnet new viewstart
    dotnet new viewimports

This creates two files that ASP.NET Core MVC will use to generate the views. Open `_ViewImports.cshtml` and change the namespace to `Uno`. We'll need to make one more change to the `Views` folder; create a `Shared` folder within it, and create a file in that folder called `_Layout.cshtml`. (This is the file to which `_ViewStart.cshtml` refers.) We will create a skeleton in that file as well.

    [lang=html]
    <!DOCTYPE html>
    <html>
    <head>
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1.0" />
        <title>@ViewData["Title"] - Uno</title>
    </head>
    <body>
        @RenderBody()
    </body>
    </html>

#### Testing

One thing we can test at this point is our sessions; are they working correctly? If we change `HomeController.Index` to look like this...

    [lang=csharp]
    // add
    using Microsoft.AspNetCore.Http;
    // ...
        public IActionResult Index() {
            HttpContext.Session.SetInt32("Count", (HttpContext.Session.GetInt32("Count") ?? 0) + 1);
            return Content($"You have viewed this index {HttpContext.Session.GetInt32("Count")} times this session");
        }

...we can view the page, refresh it, and watch the counter increase. We can also look in RavenDB and see the `CacheEntries` collection document being changed. The data in it is base-64 encoded, so we can't read it, but we can see the update and expiration changing. Using an incognito or private browser tab will be best, as the session cookie will go away once you close it.

#### Data Seeding

One final task we have to do is seed our test data. We'll do this procedurally in a method on `HomeController`, which we'll delete the next time we open it in the next step.

#### Conclusion

This is probably one of the most plumbing-like steps in the 

----
[Back to step 4](../step4)
