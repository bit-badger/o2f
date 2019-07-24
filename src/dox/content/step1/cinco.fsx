(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Freya.Core/lib/netstandard2.0/Freya.Core.dll"
#r "../../../packages/Freya.Machines.Http/lib/netstandard2.0/Freya.Machines.Http.dll"
#r "../../../packages/Freya.Routers.Uri.Template/lib/netstandard2.0/Freya.Routers.Uri.Template.dll"
#r "../../../packages/Freya.Types.Uri.Template/lib/netstandard2.0/Freya.Types.Uri.Template.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Owin/lib/netstandard2.0/Microsoft.AspNetCore.Owin.dll"
#r "../../../packages/Microsoft.AspNetCore.Server.Kestrel/lib/netstandard2.0/Microsoft.AspNetCore.Server.Kestrel.dll"
#r "../../../packages/Microsoft.AspNetCore.Server.Kestrel.Core/lib/netstandard2.0/Microsoft.AspNetCore.Server.Kestrel.Core.dll"

(**
### Cinco - Step 1

Let's take a look at the final framework we'll implement, Freya. As we've done the previous 4 times, ensure our project
is `Cinco.fsproj`, and ensure the top `PropertyGroup` element looks like this.

    [lang=xml]
    <PropertyGroup>
      <AssemblyName>Cinco</AssemblyName>
      <VersionPrefix>2.0.0</VersionPrefix>
      <OutputType>Exe</OutputType>
      <TargetFramework>netcoreapp2.2</TargetFramework>
      <RootNamespace>Cinco</RootNamespace>
    </PropertyGroup>

We'll also need to add Freya to `paket.dependencies`, then add a `paket.references` file with three entries:

    Freya
    Microsoft.AspNetCore.Owin
    Microsoft.AspNetCore.Server.Kestrel

And, we'll rename `Program.fs` to `App.fs` and tell the compiler about it:

    [lang=xml]
    <ItemGroup>
      <Compile Include="App.fs" />
    </ItemGroup>

Now, let's actually write `App.fs`:
*)
namespace Cinco

open Freya.Core
open Freya.Machines.Http
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
(**
`Freya.Core` makes the `OwinMidFunc` module visible; `Freya.Machines.Http` provides the `freyaMachine` computation
expression and the `Represent` module, which allows us to define our request-response.
*)
module WebApp =
  let hello =
    freyaMachine {
      handleOk (Represent.text "Hello World from Freya")
      }
(**
This code defines a machine which always returns our greeting in its OK response.
*)
[<RequireQualifiedAccess>]
module Configure =
  let app (app : IApplicationBuilder) =
    let freyaOwin = OwinMidFunc.ofFreya WebApp.hello
    app.UseOwin (fun p -> p.Invoke freyaOwin) |> ignore
  
module App =
  [<EntryPoint>]
  let main _ =
    use host =
      WebHostBuilder()
        .UseKestrel()
        .Configure(System.Action<IApplicationBuilder> Configure.app)
        .Build()
    host.Run()
    0
(**
The `Configure.app` function is similar to other OWIN pipeline configurations we've used, although Freya provides the
`OwinMidFunc` module to convert a Freya function into an OWIN function. The `main` function is identical to the one we
used in **Quatro**.

You may be wondering why Freya is last, if we're going from less functional to more functional - especially with how
terse **Quatro** ended up for this step. The answer is in the underlying implementation. Giraffe has a lot of defaults
and convenience functions, including that `text` handler we used for our output; it then passes the results of these
epxressions to ASP.NET Core for processing. Freya handles things completely differently; its `HttpMachine` starts out
with nothing, and only requires processing for the code it handles. If you look at `WebApp.hello`, there is no logic in
it to prevent an `200 OK` response; no redirection, no authorization checks, no rate limits, etc. It does not pass that
information off to ASP.NET Core; it simply returns that value because nothing has prevented it.

We will see machines that have more decisions than this one, and do more than just return text; when we do, you will
see how each decision point is stated explicitly in that machine.

To see the results, `dotnet run` and open localhost:5000 to observe our Hello World message.

---
[Back to Step 1](../step1)
*)