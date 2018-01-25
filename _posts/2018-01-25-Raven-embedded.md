---
layout: post
title:  "RavenDB Embedded"
date:   2018-01-25
categories: [ravendb]
---

RavenDB is document based database with **transactions (!)** and indexing features (there is more, but those stands out) that can be hosted in many ways: as service, in Docker container (see my project [RavenCage](https://github.com/pizycki/RavenCage-3.5/)) or in embedded mode.

Here I'll describe how you can quickly setup RavenDB as embedded instance (no installation required, can also work in memory).

## Let's go

1. Create new console/web app (I choosed console)

2. Install RavenDB at version **< 4.0**. (Not sure if 4.0 has embedded mode at all.)

```powershell
Install-Package RavenDB.Embedded -Version 3.5.5
```

3. Make sure `Raven.Studio.Html5.zip` is in project and marked as `Content` with `Copy` option selected. 

4. Copy/Paste configuration
```csharp
class Program
{
  static void Main(string[] args)
  {
    var store = GetRavenDB();
    
    // Once reached here, you can browse http://localhost:8888 for RavenDB Studio
    
    System.Console.ReadKey();
  }

  public static IDocumentStore GetRavenDB(bool useDisk = true)
  {
    var db = new EmbeddableDocumentStore
    {
      DataDirectory = "Data",
      UseEmbeddedHttpServer = true,
      RunInMemory = !useDisk,
      Configuration =
      {
        Port = 8888,
        //HostName = "localhost"
        AnonymousUserAccessMode = AnonymousUserAccessMode.All
      },
    };

    NonAdminHttp.EnsureCanListenToWhenInNonAdminContext(8888);

    db.Initialize();

    return db;
  }
}
```

**Note:** Don't dispose `EmbeddableDocumentStore` instance. The most common pattern is to use it as Singleton.

5. Run your app, but remember to do that with Administrator privileges.

## That's it !

This quick bootstrap let's you setup any persistance to your app in no time! 

But remember, for more serious things (like new Facebook and such), you should go for service hosted RavenDB or even consider changig it for another, more modern technology. Say [Maven](https://maven.apache.org/).
