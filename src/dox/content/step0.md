## Set Up the Environment

Before we write any code, there are a few things we need to get set up first.

### .NET Environment

This was written targeting [.NET Core 2.2](https://dotnet.microsoft.com/download). Ensure you have the latest version installed; a beta or full version of 3.0 should also work. _(If it does not, this comment will be updated.)_

### Code Editor

For Windows, macOS, and Linux, [Visual Studio Code](https://code.visualstudio.com/) is a great choice. It supports all the languages we will be using, and with the [C#](https://marketplace.visualstudio.com/items?itemName=ms-vscode.csharp) and [Ionide-fsharp](https://marketplace.visualstudio.com/items?itemName=Ionide.Ionide-fsharp) plugins, provide a very nice editing experience.

You are free to use Visual Studio if you prefer; be sure to have at least version 2019, though, as we'll be using some C# 7 and F# 4.6 features.

### Package Manager

C# developers are familiar with NuGet, .NET's package management system. For our projects, though, we are going to use one developed by the F# community called [Paket](https://fsprojects.github.io/Paket/index.html). It has several benefits, and the biggest one we'll be using is a single restore for all 5 projects' dependencies.

Paket can be installed in a project, but it can also be installed as a `dotnet tool`. These instructions are writting assuming that it is installed this way. If you wish to install it at the project level, follow their instructions; otherwise, from a command prompt, enter

    dotnet tool install --global Paket

Then, create a `src` directory. Within that directory, execute

    paket init

This should create a `paket.dependencies` file and a `paket-files` directory; this is where we'll leave them for now. If you are following along and committing your work to a source code repository, `paket-files` can be excluded from source control, but `paket.dependencies` needs to be tracked.

### Database

[RavenDB](https://ravendb.net/) has downloads for each of the main operating systems on which .NET Core is supported. The .zip file can be extracted to a directory under your control, and `run.ps1` or `run.sh` runs the server in interactive mode. You'll only need to run the database server when you're running an application or looking at data through RavenDB Studio, their outstanding web front end.

Running an extracted archive like this, the server will run for 7 days without a license. Under the "Pricing" tab on their site, they list the licenses that are available, including two that are free. Near the bottom of the page, you'll find the "Developer" license; this is completely acceptable for our uses in this project. Their staff will e-mail you a JSON blob that you can paste into the server, and it will recognize that it's licensed to a developer.

---
[Back](./)
