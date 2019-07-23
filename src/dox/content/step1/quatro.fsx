(*** hide ***)
#r "../../../packages/Giraffe/lib/netstandard2.0/Giraffe.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting/lib/net451/Microsoft.AspNetCore.Hosting.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/net451/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/net451/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Owin/lib/net451/Microsoft.AspNetCore.Owin.dll"
#r "../../../packages/Microsoft.AspNetCore.Server.Kestrel/lib/net451/Microsoft.AspNetCore.Server.Kestrel.dll"

(**
### Quatro - Step 1

Having [already made the leap to F#](./tres.html), we will now take a look at Giraffe. It was created by Dustin Moris
Gorski as a [Suave](https://suave.io)-like functional abstraction over ASP.NET Core. It allows composable functions and
expressive configuration, while then delegating the work to the same libraries that C# applications use. Make sure the
projet file name is `Quatro.fsproj`, and ensure the top looks like the other projects:
  
    [lang=xml]
    <PropertyGroup>
      <AssemblyName>Quatro</AssemblyName>
      <VersionPrefix>2.0.0</VersionPrefix>
      <OutputType>Exe</OutputType>
      <TargetFramework>netcoreapp2.2</TargetFramework>
      <RootNamespace>Quatro</RootNamespace>
    </PropertyGroup>

To be able to develop this project, we need to add Giraffe to `paket.dependencies`. Thanks to Giraffe's dependencies,
our `paket.references` file for this project is the easiest we've seen thus far - one line.

    Giraffe

Run `paket install` to download Giraffe and its dependencies _(which will be many more than we've seen with previous
dependencies, as Giraffe depends on the the entire ASP.NET Core framework, not just the pieces we brought in for
**Uno** )_, and let it fix up `Quatro.fsproj`. We'll also go ahead and rename `Program.fs` to `App.fs` to remain
consistent among the projects, and tell the compiler about it:

    [lang=text]
    <ItemGroup>
      <Compile Include="App.fs" />
    </ItemGroup>

Now, let's actually write `App.fs`:
*)
namespace Quatro

open Freya.Core
open Freya.Machines.Http
open Freya.Routers.Uri.Template
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
(**
`Freya.Core` gives us the `freya` computation expression, which we will use for the main part of our request handling.
`Freya.Machines.Http` provides the `freyaMachine` computation expression, which allows us to define our
request-response.  `Freya.Routers.Uri.Template` provides the `freyaRouter` computation expression, where we assign an
HTTP machine to a URL route pattern.

Continuing on...
*)
module App =
  let hello =
    freya {
      return Represent.text "Hello World from Freya"
      }

  let machine =
    freyaMachine {
      handleOk hello
      }

  let router =
    freyaRouter {
      resource "/" machine
      }
(**
This code uses the three expressions described above to define the response (hard-coded for now), the machine that uses
it for its OK response, and the route that uses the machine.

Still within `module App =`...
*)
  type Startup () =
    member __.Configure (app : IApplicationBuilder) =
      let freyaOwin = OwinMidFunc.ofFreya (UriTemplateRouter.Freya router)
      app.UseOwin (fun p -> p.Invoke freyaOwin) |> ignore

  [<EntryPoint>]
  let main _ =
    use host = (new WebHostBuilder()).UseKestrel().UseStartup<Startup>().Build()
    host.Run()
    0
(**
This is the familiar `Startup` class from Tres, except that the `Configure()` method uses the Freya implementation
instead of the Nancy implementation.  Notice that the middleware function uses the router as the hook into the
pipeline; that is how we get the OWIN request to be handled by Freya.  Notice how much closer to idiomatic F# this code
has become; the only place we had to `ignore` anything was the "seam" where we interoperated with the OWIN library.

`dotnet run` should succeed at this point, and localhost:5000 should display our Hello World message.

[Back to Step 1](../step1)
*)