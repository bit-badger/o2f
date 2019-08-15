### Dos - Step 4

#### Dependencies

There are fewer changes here than there were for **Uno**; in fact, we really only need one more package. Add `nuget Nancy.Session.RavenDB` to `paket.dependencies`, add `Nancy.Session.RavenDB` to `paket.references`, and run `paket install`.

#### Bootstrapper Updates

The package we've added is part of the [`Nancy.Session.Persistable` project](https://github.com/danieljsummers/Nancy.Session.Persistable), a set of persistence providers I wrote for Nancy that allow strongly-typed access to session objects, and provide for several different stores of session data. We need to configure this in our bootstrapper, though. We also need to update our `DataConfig` class to handle the configuration of a certificate file and password, to support secure connections to RavenDB.

In `DataConfig.cs`, above the definition for `Urls`:
    
    [lang=csharp]
    public string Certificate { get; set; }
    public string Password { get; set; }

Our bootstrapper is going to look significantly different than it did before. When we set up RavenDB, we put all the logic in `ConfigureApplicationContainer`, even though its main purpose it to register dependencies in the IoC container. To actually affect the Nancy request execution pipeline, we need to override `ApplicationStartup` as well. Since we'll need our `IDocumentStore` instance in both places, we'll make it a `private static` property of the bootstrapper.

First, we need several new `using`s:

    [lang=csharp]
    using Nancy.Bootstrapper;
    using Nancy.Session.Persistable;
    using Nancy.Session.RavenDB;
    using System;
    using System.Security.Cryptography.X509Certificates;

Then, we'll create our `Store` property:

    [lang=csharp]
    private static IDocumentStore Store = new Lazy<IDocumentStore>(() =>
    {
        var cfg = JsonConvert.DeserializeObject<DataConfig>(File.ReadAllText("data-config.json"));
        return new DocumentStore
        {
            Urls = cfg.Urls,
            Database = cfg.Database,
            Certificate = string.IsNullOrEmpty(cfg.Certificate)
                ? null
                : new X509Certificate2(cfg.Certificate, cfg.Password)
        }.Initialize();
    }).Value;

Finally, our two overridden methods:

    [lang=csharp]
    protected override void ConfigureApplicationContainer(TinyIoCContainer container)
    {
        base.ConfigureApplicationContainer(container);
        container.Register(Store);
    }
    protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
    {
        base.ApplicationStartup(container, pipelines);
        IndexCreation.CreateIndexes(typeof(Categories_ByWebLogIdAndSlug).Assembly, Store);
        PersistableSessions.Enable(pipelines, new RavenDBSessionConfiguration(Store));
    }

Note that we've moved index creation to application startup; configuring the container simply registers the dependency. Also, since `ApplicationStartup` also provides the container, we could have registered the store without the `private static` property, then used `var store = container.Resolve<IDocumentStore>();` in the startup method to obtain its instance.

At this point, we should be able to start the application, then use RavenDB Studio to look at the indexes for the `O2F2` database; if you find `Sessions/ByLastAccessed`, the session store has been initialized successfully.

#### Testing

We can easily create a home page counter, like we did for **Uno**. In `HomeModule.cs`, add `using Nancy.Session.Persistable;` to the top; this exposes the `PersistableSession()` extension method on Nancy's `Request` object. This view of the session will allow us to get strongly-typed items from the session. This allows us to change the function mapped to `/` to:

    [lang=csharp]
    Get("/", _ => {
        var count = (Request.PersistableSession().Get<int?>("Count") ?? 0) + 1;
        Request.PersistableSession()["Count"] = count;
        return $"You have visited this page {count} times this session";
    });

If you run the site, you should be able to refresh and see the counter grow. You should also be able to look at the most recently updated session document in RavenDB Studio, and see the `"Count"` property in the data for that session.

#### Data Seeding

As with **Uno**, we'll make an endpoint on our `HomeModule` to seed the data, which we can delete when we get to step 5. Rather than go through it here, you can [view the completed method](https://github.com/bit-badger/o2f/tree/master/src/2-Dos/Modules/HomeModule.cs#L17) to see the hard-coded values we're storing. Notice that we provided an instance of `IDocumentStore` in the constructor; TinyIoC will resolve that for us and provide us with the instance we registered in our bootstrapper.

Run the application, then visit http://localhost:5000/seed; you should see "All done!". At that point, you should also be able to use RavenDB Studio to look at the documents that were added during this step.

#### Conclusion

There was a lot less to do in this step for Nancy than there was for ASP.NET Core MVC. However, as we're using most of the defaults for Nancy and our session store, the SDHP helps us write less code. Also, we haven't set up anything for the Super Simple View Engine at this step, as we're returning strings right now from our module endpoints; we'll tackle that during step 6.

---
[Back to step 4](../step4)
