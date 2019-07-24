(*** hide ***)
#r @"C:\Windows\Microsoft.NET\assembly\GAC_MSIL\netstandard\v4.0_2.0.0.0__cc7b13ffcd2ddd51\netstandard.dll"
#r "../../../packages/Giraffe/lib/netstandard2.0/Giraffe.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.dll"
#r "../../../packages/Microsoft.AspNetCore.Hosting.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Hosting.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Http.Abstractions/lib/netstandard2.0/Microsoft.AspNetCore.Http.Abstractions.dll"
#r "../../../packages/Microsoft.AspNetCore.Server.Kestrel/lib/netstandard2.0/Microsoft.AspNetCore.Server.Kestrel.dll"
#r "../../../packages/Microsoft.AspNetCore.Server.Kestrel.Core/lib/netstandard2.0/Microsoft.AspNetCore.Server.Kestrel.Core.dll"

(**
### Quatro - Step 1

Having [already made the leap to F#](./tres.html), we will now take a look at Giraffe. It was created by Dustin Moris
Gorski as a [Suave](https://suave.io)-like functional abstraction over ASP.NET Core. It allows composable functions and
expressive configuration, while then delegating the work to the same libraries that C# applications use. Make sure the
project file name is `Quatro.fsproj`, and ensure the top looks like the other projects:
  
    [lang=xml]
    <PropertyGroup>
      <AssemblyName>Quatro</AssemblyName>
      <VersionPrefix>2.0.0</VersionPrefix>
      <OutputType>Exe</OutputType>
      <TargetFramework>netcoreapp2.2</TargetFramework>
      <RootNamespace>Quatro</RootNamespace>
    </PropertyGroup>

To be able to develop this project, we need to add Giraffe to `paket.dependencies`. Create `paket.references` with the
following packages:

    Giraffe
    Microsoft.AspNetCore.Hosting
    Microsoft.AspNetCore.Server.Kestrel

Run `paket install` to download Giraffe and its dependencies _(which will be many more than we've seen with previous
dependencies, as Giraffe depends on the the entire ASP.NET Core framework, not just the parts that **Uno** required)_,
and let it fix up `Quatro.fsproj`. We'll also go ahead and rename `Program.fs` to `App.fs` to remain consistent among
the projects, and tell the compiler about it:

    [lang=text]
    <ItemGroup>
      <Compile Include="App.fs" />
    </ItemGroup>

Giraffe uses the concept of a
[handler function](https://github.com/giraffe-fsharp/Giraffe/blob/master/DOCUMENTATION.md#httphandler) to compose web
applications. Take a look at that text under that heading, because, in F#, the types are your guide. With types defined
clearly, it can be easy to figure out how the composition should happen. Think of it like Lego&reg; bricks; you know
what blocks you can assemble based on the shape of the bricks and their edges.

In F# notation, `->` describes parameters for a function that can be provided one at a time. Let's suppose we have a
`repeat` function that takes a string and a number, and returns the string repeated that number of times; so,
`repeat "abc" 3` would return `"abcabcabc"`. The type signature of this function is `string -> int -> string`. If we
call it with just a string, ex. `let x = repeat "bob"`, `x` will have the type signature `int -> string`. In effect, `x`
is a function that, given an integer, will return that number of "bob"s. This concept is called currying, and can be
helpful when you need to call a function many times with most of the parameters the same - that is, if the parameters
you're changing are the ones at the end of the parameter list. 

With that being said, the signature for an `HttpHandler` is
    
    [lang=fsharp]
    (HttpContext -> Task<HttpContext option>) -> HttpContext -> Task<HttpContext option>

Ignoring `Task` (after noting that this means it's using .NET's task-based asynchrony), what we have is a function
definition that takes an `HttpContext` and returns an `HttpContext option`, which has an `HttpContext` as its parameter
and an `HttpContext option` as its return value. The `HttpContext` as the parameter will be fed into the input of the
initial function, and its `HttpContext option` will be returned as the call's output. It seems convoluted, but think of
the first function (the part in parenthesis) as a process definition, and the middle `HttpContext` as the execution
parameter that kicks the process off. You can have as many processes defined as you want, and you can chain them
together; once a request generates an actual context, it is run through this process chain.

The `option` part is new. In the [intro](../intro.html), I mentioned Haskell's `Maybe` monad; this is F#'s version of
that pattern. `option`s can be `Some` or `None`, indicating whether the value is present or not. We'll dig into them
more the further along we get, but for now, we'll just see how Giraffe uses this. If the `HttpHandler` returns `Some`,
procesing continues. (The value of the `Some` it returns is the same `HttpContext`, which may have been modified by the
handler.) If the `HttpHandler` returns `None`, it means the handler could not do anything with the request, and Giraffe
will not handle it. Giraffe provides a composition operator `>=>` that allows us to compose `HttpHandler`s together,
and handles feeding the output from one into the input of the next one.

With all that - Giraffe includes some built-in handlers for common tasks, and returning text is one of them. Here's our
handler function...

    [lang=fsharp]
    text "Hello World from Giraffe"

...but we're not going to write it just yet. To this point, we've used a `Startup` class to configure our environment.
Creating a "magic" class to do our configuration isn't really the functional way, though; for this version, we'll
configure the web host builder with custom functions instead.

This gives us the most terse single-file solution of the 5; here is `App.fs` in its entirety:
*)
namespace Quatro

open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting

[<RequireQualifiedAccess>]
module Configure =
  let app (app : IApplicationBuilder) =
    app.UseGiraffe (text "Hello World from Giraffe")
    
module App =
  [<EntryPoint>]
  let main _ =
    use host =
      WebHostBuilder()
        .UseKestrel()
        .Configure(System.Action<IApplicationBuilder> Configure.app)
        .Build ()
    host.Run()
    0
(**
We use `RequireQualifiedAccess` for our `Configure` module to prevent its names from conflicting with others. For this
step, it's probably overkill, but it will be helpful when we have more than one thing we are configuring. Notice the
`app.UseGiraffe` call; it uses our handler inline. In the future, we'll wire in a router than handles many URLs, yet it
still ends up as an `HttpHandler`.

Also of note is that this implementation, though being in F#, has no `ignore` calls. `.UseGiraffe` is designed to be at
the end of the configuration chain, so instead of returning the `IApplicationBuilder`, it returns nothing.

Finally, the `System.Action` wrapping of our `Configure.app` function is, strangely, necessary. Usually, a function that
returns unit `()` is recognized as a `void` function, but in this one particular case, it isn't. I suspect there may be
conflicting overloads which the compiler can't resolve, but I don't know that for sure.

`dotnet run` should succeed at this point, and localhost:5000 should display our Hello World message.

---
[Back to Step 1](../step1)
*)