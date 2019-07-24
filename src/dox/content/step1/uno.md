### Uno - Step 1

_NOTE: While there is a "web" target for C#, it pulls in a lot of files that I'd rather not go through and explain. We will not be using Entity Framework for anything, and though this application will use some of the Identity features of ASP.NET Core MVC, we will not be using its membership features. Since all of that is out of scope for this effort, and all of this is in the "web" template, we won't use it._  😃

To start, we'll make sure the `.csproj` file is named `Uno.csproj`. Then, under the first `PropertyGroup` item, we'll add a few items; when we're done, it should look like this:

    [lang=xml]
    <PropertyGroup>
      <AssemblyName>Uno</AssemblyName>
      <VersionPrefix>2.0.0</VersionPrefix>
      <OutputType>Exe</OutputType>
      <TargetFramework>netcoreapp2.2</TargetFramework>
    </PropertyGroup>

When we set up our environment, we created an empty `paket.dependencies` file in the `src` directory. To add dependencies to this project, though, we'll need to add a `paket.references` file in the directory for this project. It is a plain-text file with package names listed, one per line. We cannot reference packages that are not listed as dependencies, though, so we'll need to add the following two lines to `paket.references` in the parent folder:

    nuget Microsoft.AspNetCore.Owin
    nuget Microsoft.AspNetCore.Server.Kestrel

Paket uses the NuGet package repository as one of its sources, so these are the same packages that we get when we install them via the NuGet Package Manager. In our new `paket.references` file, we'll create it with the following two lines:

    Microsoft.AspNetCore.Owin
    Microsoft.AspNetCore.Server.Kestrel

Finally, from the parent directory, run `paket install`. This will go through, install the new dependencies, look for any subdirectories with referenced packages, and modify the `.csproj` or `/fsproj` files with those references.

> A side note about the files generated by the above command. It will create a `.paket` directory in the `src` directory; this should be source controlled. It downloads the packages to the `packages` directory; this should be excluded from source control. It also creates a `paket.lock` file; this is the complete dependency tree, and should be source controlled.

Paket also made a change in our `Uno.csproj` file.

    [lang=xml]
    <Import Project="..\.paket\Paket.Restore.targets" />

This takes the place of the `PackageReference` items you would normally see for package references; the `.targets` file is an MSBuild task that lets Paket resolve the dependencies against what is in the `packages` directory.

Our next step is to create the `Startup.cs` file, which is the standard configuration for ASP.NET Core projects. Within its `Configure` method, we'll do a very basic lambda to return a string:

    [lang=csharp]
    public void Configure(IApplicationBuilder app) =>
        app.Run(async context => await context.Response.WriteAsync("Hello World from ASP.NET Core"));

(We put in using statements for `Microsoft.AspNetCore.Builder` to make the `IApplicationBuilder` visible and `Microsoft.AspNetCore.Http` to expose the `WriteAsync()` method on the `Response` object.)

We'll rename `Program.cs` to `App.cs`. _(Why? Well - why not?)_ Then, within the `Main()` method, we'll construct a Kestrel instance and run it.

    [lang=csharp]
    using (var host = new WebHostBuilder().UseKestrel().UseStartup<Startup>().Build())
    {
        host.Run();
    }

We'll need to add `using Microsoft.AspNetCore.Hosting;` to bring in the `WebHostBuilder` class. Also, most demos don't show the web host wrapped in a using block; it's `IDisposable`, though, so it's a good idea.

At this point, `dotnet run` should give us a successful startup, and browsing to localhost:5000 returns our greeting.

---
[Back to Step 1](../step1)