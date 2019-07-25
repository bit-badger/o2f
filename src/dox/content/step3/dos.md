### Dos - Step 3

This step follows the same general path as [Uno](./uno.html); the main difference is in how we configure dependency injection. Go ahead and create the `O2F2` database using RavenDB Studio.

#### Dependencies

The only change we need here is to add the RavenDB client package to `paket.references`, then run `paket install`.

    RavenDb.Client

We'll also bring across the entire `Data` directory we created in **Uno** during this step. We'll be able to use `Collection.cs` and the indexes as is (except for changing the namespace to `Dos.Data`).

#### Configuring the Connection

Since `appsettings.json` is a .NET Core thing, we will not use it here.  We can still use JSON to configure our connection, though; here's the `data-config.json` file:

    [lang=json]
    {
      "Url": "http://localhost:8080",
      "Database": "O2F2"
    }

...and add the following to `Dos.csproj` so that the compiler will copy it to the output directory:

    [lang=xml]
    <ItemGroup>
        <None Update="data-config.json">
            <CopyToOutputDirectory>Always</CopyToOutputDirectory>
        </None>
    </ItemGroup>

We'll also create a small POCO into which we can deserialize this JSON. Below is `Data/DataConfig.cs`: 

    [lang=csharp]
    using Newtonsoft.Json;

    namespace Dos.Data
    {
        public class DataConfig
        {
            public string Url { get; set; }
            public string Database { get; set; }

            [JsonIgnore]
            public string[] Urls => new[] { Url };
        }
    }

_(`Urls` is just a convenience property to wrap the `Url` property as an array; the RavenDB client can take an array of URLs if the application needs to connect to a cluster of servers.)_ Now we can read `data-config.json` in and get our settings from this class. But first, we'll need to make some other changes.

#### Dependency Injection

With Nancy, if you want to add forks to the SDHP, you have to provide a bootstrapper that will handle the startup code. For most purposes, the best way is to simply override `DefaultNancyBootstrapper`; that way, any code you don't provide will use the default, and you can call `base` methods from your overridden ones, so all the SDHP magic continues to work.

Here's the custom bootstrapper we'll use:

    [lang=csharp]
    using Dos.Data;
    using Dos.Data.Indexes;
    using Nancy;
    using Nancy.TinyIoc;
    using Newtonsoft.Json;
    using Raven.Client.Documents;
    using Raven.Client.Documents.Indexes;
    using System.IO;

    namespace Dos
    {
        class DosBootstrapper : DefaultNancyBootstrapper
        {
            public DosBootstrapper() : base() { }

            protected override void ConfigureApplicationContainer(TinyIoCContainer container)
            {
                base.ConfigureApplicationContainer(container);

                var cfg = JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText("data-config.json"));
                var store = new DocumentStore
                {
                    Urls = cfg.Urls,
                    Database = cfg.Database
                };
                container.Register(store.Initialize());
                IndexCreation.CreateIndexes(typeof(Categories_ByWebLogIdAndSlug).Assembly, store);
            }
        }
    }

This is a bit different from the ASP.NET Core implementation. We do our own configuration, through reading files and deserializing JSON, instead of relying on the different file and environment options we did in **Uno**. The `IDocumentStore` initialization is similar, though, and `container.Register` is how we register the singleton with Nancy's DI container. Finally, the index creation is exactly the same.

What is different, though, is that in **Uno**, all that went in `Startup.cs`. We do still need to make a change to that file, though, so it will know to use our custom bootstrapper instead of Nancy's default one. This looks like:

    [lang=csharp]
    public void Configure(IApplicationBuilder app) =>
        app.UseOwin(x => x.UseNancy(options => options.Bootstrapper = new DosBootstrapper()));

At this point, you should be able to `dotnet run` and, once the console says that it's listening, you should be able to see the indexes in the `O2F2` database.

---
[Back to Step 3](../step3)