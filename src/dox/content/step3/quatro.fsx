(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Giraffe/lib/netstandard2.0/Giraffe.dll"
//#r "../../../packages/Microsoft.AspNetCore.Hosting/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
//#r "../../../packages/Microsoft.AspNetCore.Owin/lib/netstandard2.0/Microsoft.AspNetCore.Owin.dll"
#r "../../../packages/Microsoft.Extensions.Configuration/lib/netstandard2.0/Microsoft.Extensions.Configuration.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.Abstractions/lib/netstandard2.0/Microsoft.Extensions.Configuration.Abstractions.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.EnvironmentVariables/lib/netstandard2.0/Microsoft.Extensions.Configuration.EnvironmentVariables.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.FileExtensions/lib/netstandard2.0/Microsoft.Extensions.Configuration.FileExtensions.dll"
#r "../../../packages/Microsoft.Extensions.Configuration.Json/lib/netstandard2.0/Microsoft.Extensions.Configuration.Json.dll"
#r "../../../packages/Microsoft.Extensions.DependencyInjection/lib/netstandard2.0/Microsoft.Extensions.DependencyInjection.dll"
#r "../../../packages/Microsoft.Extensions.DependencyInjection.Abstractions/lib/netstandard2.0/Microsoft.Extensions.DependencyInjection.Abstractions.dll"
#r "../../../packages/Microsoft.Extensions.FileProviders.Abstractions/lib/netstandard2.0/Microsoft.Extensions.FileProviders.Abstractions.dll"
#r "../../../packages/Newtonsoft.Json/lib/netstandard2.0/Newtonsoft.Json.dll"
#r "../../../packages/RavenDb.Client/lib/netstandard2.0/Raven.Client.dll"
(**
### Quatro - Step 3

Once again, we need to add the RavenDB dependency to `paket.references`; we'll also need to add some ASP.NET Core
references as well. The new references are:

    Microsoft.Extensions.Configuration.FileExtensions
    Microsoft.Extensions.Configuration.Json
    Microsoft.Extensions.Options.ConfigurationExtensions
    RavenDb.Client

The, run `paket install` to register these as as part of this project.

#### Configuring the Connection and Dependency Injection

As we're back in ASP.NET Core land, we'll do these together. We'll return to `appsettings.json` for our configuration;
create that file with these values.

    [lang=json]
    {
      "RavenDB": {
        "Url": "http://localhost:8080",
        "Database": "O2F4"
      }
    }

We'll also need to make sure the file is copied to the build directory, so add the following to `Quatro.fsproj`, inside
the list of compiled files:

    [lang=xml]
    <Content Include="appsettings.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>

In [step 2](../step1/quatro-cinco.html), we mentioned that we are going to change most of our namespace declarations to
modules instead. In `App.fs`, let's change `namespace Quatro` to `module Quatro.App`, remove the `module App`
declaration near the bottom of the file, and move `let main` out to column 1. After that, we will convert the
initialization and DI logic to the function-based initialization we used in step 1. When the functions get pulled into
the `WebHostBuilder`, their shapes may be a bit different than the `Startup` class-based ones.

Here is what our new `Configure` module looks like:

*)
(*** hide ***)
namespace Quatro
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
(** *)
[<RequireQualifiedAccess>]
module Configure =
  
  open Microsoft.Extensions.Configuration
  open Microsoft.Extensions.DependencyInjection
  open Raven.Client.Documents
  
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

  let app (app : IApplicationBuilder) =
    app.UseGiraffe (htmlString "Hello World from Giraffe")
(**
Then, within our `main`, we'll call these new functions; our `host` definition now looks like:
    
    [lang=fsharp]
    use host =
      WebHostBuilder()
        .ConfigureAppConfiguration(Configure.configuration)
        .UseKestrel()
        .ConfigureServices(Configure.services)
        .Configure(System.Action<IApplicationBuilder> Configure.app)
        .Build ()

Notice that we've moved our `open` statements for types that we do not need outside of the `Configure` module to appear
inside it. This is a way that F# can help isolate namespace conflicts. If you have ever worked on an application that
decided to name a type using a common term (`Table` comes to my mind), the few namespaces you open, the less chance you
have of running into a competing definition of the same type.

#### Ensuring Indexes Exist

We'll bring across `Indexes.fs` from **Tres**, changing it to `Quatro`, and adding it to the compile order between
`Domain.fs` and `App.fs`. Then, we can go back to our `Configure.services` function, and add the `IndexCreation` call
to create our indexes:

    [lang=fsharp]
    IndexCreation.CreateIndexes (typeof<Categories_ByWebLogIdAndSlug>.Assembly, store)

(We need to open `Raven.Client.Documents.Indexes` and `Indexes` (our module) for this call to work.)

#### Revising the Data Model

**TODO: stopped here**

---
[Back to Step 3](../step3)
*)