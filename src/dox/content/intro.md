## objects |> functions Introduction

### Background

A skilled C# developer can write some pretty incredible software. It is a rich language that has many of the features of other object-oriented (OO) languages, without many of the pain points that those other languages seem to have. The .NET family of frameworks have an expansive set of library functions that allow developers to quickly get their ideas from concept to executable code.

Within the OO world, there are some downsides. All but the smallest of types are implemented as reference types; and, to the chagrin of many a developer, references can be null (and likely will, at the most inconvenient time). All OO languages provide a way to check if the instance is pointing to a null reference, though, so there is a way around that - but you have to write code for it every time you want to check. Objects are also usually mutable; the general flow is to write `var x = new MyObject();`, and then set its properties as `x.MyProp = "abc";`. Combine this mutability with reference passing, though, and you have the perfect recipe for strange behavior and unexpected side effects.

C# has begun embracing some functional programming concepts; [LINQ](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/linq/introduction-to-linq-queries) and its anonymous lambda functions that it converts to expressions are a good example. If you've ever written `from foo in foos where foo.Count > 7 select foo.Name`, or `foos.Where(foo => foo.Count > 7).Select(foo => foo.Name)`, you've written a filter/map function chain (possibly without realizing it). [C# 7](https://docs.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-7) even brought a new syntax for defining a function local to a method, which allows for structuring code in functions more succinctly.

The developers of F# looked at the problems that OO had, and noticed that these were problems that functional languages did not have. Some of the concepts were similar, but there were fewer pitfalls. For example, Haskell has a `Maybe` monad _(for now, think of "monad" as a type pattern)_ that is used when a value may or may not be present; values can't be null, because if they can be missing, they're wrapped in a `Maybe`. They also saw mutability as not only a source of latent bugs, but as an impediment to distributed and parallel processing. If a process flow relies on state that is mutated as the object moves through the process, it must be done linearally. The downside of these "other" functional languages, though, is that they don't run under .NET, so they could not take advantage of this large repository of reusable code.

The F# developers looked to remedy that, bringing a Mathematics Language (ML)-style programming language to the .NET environment. F#-specific types are non-nullable by default, and variables must be explicitly made mutable. It provides collection types, and operators on those types, that enable set processing and expression-based program flows. Additionally, as a first-class member of the .NET family, it has OO syntax as well, and can interoperate smoothly with the majority of the concepts within .NET, including generics and task-based asynchronous processing.

### Our Mission

At this point, I doubt that you are convinced of F#'s superiority in the software space. I do hope that you're intrigued, though, and will follow along this journey as we look at how to go from object-oriented thinking to functional thinking.

As we move forward, we will be developing the same application 5 different ways. We will be building a rudimentary multi-site blogging platform from scratch. We will use libraries, but we are not going to install large frameworks, with the possible exception of the ASP.NET Core based solutions in later steps. All the applications will target .NET Core.

**Uno** will be written in C# and use [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/?view=aspnetcore-2.2) MVC as its framework, along with Razor views. This is Microsoft's flagship product in the .NET web space, so it's a good place to start. **Dos** will also be written in C#, but use [Nancy](http://nancyfx.org/), a lightweight MVC framework; Nancy includes the Super Simple View Engine, which we'll use in this project. **Tres** will be **Dos** converted to F# (with a few changes), showing off the hybrid OO/functional nature of F#. **Quatro** will be written in F# and use [Giraffe](https://github.com/giraffe-fsharp/Giraffe), a project that enables functional development of an ASP.NET Core application. It also provides the Giraffe View Engine, which we will utilize. Finally, **Cinco** will be written in F# and use [Freya](https://freya.io/), a composable, functional framework. We will use the Giraffe views from **Quatro** for this project.

Or, in table form:

<table>
  <tr>
    <th>&nbsp;</th>
    <th>Uno</th>
    <th>Dos</th>
    <th>Tres</th>
    <th>Quatro</th>
    <th>Cinco</th>
  </tr>
  <tr>
    <th>Language</th>
    <td>C#</td>
    <td>C#</td>
    <td>F#</td>
    <td>F#</td>
    <td>F#</td>
  <tr>
  <tr>
    <th>Framework</th>
    <td>ASP.NET Core</td>
    <td>Nancy</td>
    <td>Nancy</td>
    <td>Giraffe</td>
    <td>Freya</td>
  </tr>
  <tr>
    <th>Views</th>
    <td>Razor</td>
    <td>SSVE</td>
    <td>SSVE</td>
    <td>Giraffe</td>
    <td>Giraffe</td>
  </tr>
  <tr>
    <th>Server</th>
    <td>Kestrel</td>
    <td>Kestrel</td>
    <td>Kestrel</td>
    <td>Kestrel</td>
    <td>Kestrel</td>
  </tr>
  <tr>
    <th>Database</th>
    <td>RavenDB</td>
    <td>RavenDB</td>
    <td>RavenDB</td>
    <td>RavenDB</td>
    <td>RavenDB</td>
  </tr>
</table>