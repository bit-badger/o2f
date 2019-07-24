### Dos - Step 1

For this project, we'll make sure our project file is `Dos.csproj`, and modify it the way we did [for Uno](./uno.html).

    [lang=xml]
    <PropertyGroup>
      <AssemblyName>Dos</AssemblyName>
      <VersionPrefix>2.0.0</VersionPrefix>
      <OutputType>Exe</OutputType>
      <TargetFramework>netcoreapp2.2</TargetFramework>
      <RootNamespace>Dos</RootNamespace>
    </PropertyGroup>

We'll need to add Nancy to `paket.dependencies` (`nuget Nancy`), and create a `paket.references` file for **Dos** that has the same packages from **Uno**, plus Nancy. The whole file will look like:
    
    Microsoft.AspNetCore.Owin
    Microsoft.AspNetCore.Server.Kestrel
    Nancy

Then, run `paket install` to pull in the new dependency and make the necessary modifications to `Dos.csproj`.

Nancy strives to provide a Super-Duper-Happy-Path (SDHP), where all you have to do is follow their conventions, and everything will "just work." (You can also configure every aspect of it; it's only opinionated in its defaults.) One of these conventions is that the controllers inherit from `NancyModule`, and when they do, no further configuration is required. So, we create the `Modules` directory, and add `HomeModule.cs`, which looks like this:

    [lang=csharp]
    using Nancy;

    namespace Dos.Modules
    {
        public class HomeModule : NancyModule
        {
            public HomeModule() : base()
            {
                Get("/", _ => "Hello World from Nancy C#");
            }
        }
    }

Since we'll be hosting this with Kestrel (via OWIN), we still need a `Startup.cs`, though its `Configure()` method looks a bit different:

    [lang=csharp]
    public void Configure(IApplicationBuilder app) =>
        app.UseOwin(x => x.UseNancy());

(We need to add a using statement for `Nancy.Owin` so that the `UseNancy()` method is visible.)

With the exception of the namespace, the `App.cs` file is identical to the one from Uno. `dotnet run`, then go to http://localhost:5000 and see your greeting from Nancy!

---
[Back to Step 1](../step1)