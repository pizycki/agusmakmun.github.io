RavenDB Embedded

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
