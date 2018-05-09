---
layout: post
title:  "Resolving after dependency injection"
date:   2017-03-16
categories: [.NET, Autofac, IoC]
---

Let's say you have service that is responsible for creating new tenant databases. The service uses several strategies for creating database instances of different RDBMS types.

```csharp
public interface ITenantDatabaseCreator {
  void Create(/* Some args */);
}

public class MssqlTenantDatabaseCreator : ITenantDatabaseCreator {
  // MS SQL implementation
}

public class OracleTenantDatabaseCreator : ITenantDatabaseCreator {
  // Oracle implementation
}
```

We're good programmers so we inject our creators (strategies) with Autofac IoC container*. 

This is how we can do it.

```csharp
enum DatabaseType {
  Mssql, Oracle
}

public class TenantDatabaseCreateService {
  private readonly ITenantDatabaseCreator _tenantDatabaseCreator;
  
  public TenantDatabaseCreateService(ITenantDatabaseCreator tenantDatabaseCreator){
    _tenantDatabaseCreator = tenantDatabaseCreator;
  }
  
  public void CreateTenantDatabase(DatabaseType databaseType) {
    // ...
  }  
}
```

As we can see, implementation is trying to be resolved even before we know which one should be picked. How can we know which implementation should be taken?

There are many implementation registered to this interface, so even if we try to run this incomplete code, we're going to receive Autofac exception.

Injecting concrete implementation in `TenantDatabaseCreateService` constructor is too early for us.  We need some way of resolving implementation **inside** called method.

Interested how we can do it? There are some ways. Let's look at each of them.




## Delegate factory

This is one of the most common solutions when implementation must be resolved in method and not in constructor. It's quite simple and if developer has some expearience with IoC containers it will be easy for him to understand it.

We register function that accepts two arguments of types `IComponentContext` and `DatabaseType` and returns seeked implementation. The function will be injected into constructor and assigned to private class member. Methods inside the class will be able to invoke the factory function and resolve implementation.

> This might look a little bit like [Factory pattern](https://www.tutorialspoint.com/design_pattern/factory_pattern.htm). For some scenarios it would be easier to use it here instead of delegate, but in result we would loose all dependency injection features. 

This is how we can register delegate factory for our needs

```csharp
containerBuilder.RegisterType<MssqlTenantDatabaseCreator>().AsSelf();
containerBuilder.RegisterType<OracleTenantDatabaseCreator>().AsSelf();

containerBuilder.Register<Func<DatabaseType, ITenantDatabaseCreator>>(
	c => type => { // c: ComponentContext, generaly use this as builded container
      switch(type) {
          
        case DatabaseType.Mssql:
          return c.Resolve<MssqlTenantDatabaseCreator>();
            
        case DatabaseType.Oracle:
          return c.Resolve<OracleTenantDatabaseCreator>();
          
        default: throw new ArgumentOutOfRangeException("Unknown type.");
      }
	}); 
```

Using delegate factory


```csharp
public class TenantDatabaseCreateService {
  private readonly Func<DatabaseType, ITenantDatabaseCreator> _createTenantDatabaseCreator;
  
  public TenantDatabaseCreateService(createTenantDatabaseCreator) {
    _createTenantDatabaseCreator = createTenantDatabaseCreator;
  }
  
  public void CreateTenantDatabase(DatabaseType databaseType) {
    var creator = _createTenantDatabaseCreator(databaseType);
    creator.Create();
    // Further instructions ...
  }
}
```

You may ask _"What's wrong about it?"_

Resolving implementation is easy, but registering... not so much. Imagine service with hundreds of implementations. Do you really would like to generate such incredibly huge `switch` inside `Func`? Me neither.

(I know, R# can generate this switch for us, but someone has to maintain it later ;) )

**Update:**

Maybe something has changed, maybe I simply made a mistake writing this post, but I couldn't get on with solution shown above.

Below I present the sample which looks very similiar and varies only in registration details.

It feels like it's a in the middle between delegate factory and `IIndex`.

```csharp
// Based on this answer https://stackoverflow.com/a/15427548/864968

containerBuilder.RegisterType<MssqlTenantDatabaseCreator>()
                .Keyed<ITenantDatabaseCreator>(DatabaseType.Mssql);
containerBuilder.RegisterType<OracleTenantDatabaseCreator>()
                .Keyed<ITenantDatabaseCreator>(DatabaseType.Oracle);
// Don't forget about lifetime scopes !

containerBuilder.Register<Func<DatabaseType, ITenantDatabaseCreator>>(
	c => {
      var ctx = c.Resolve<IComponentContext>();
      return type => ctx.ResolveKeyed<ITenantDatabaseCreator>(type);
	}); 
```

And now it doesn't look that bad.




## Service Locator

Service Locator is considered by many as an antipattern. Ploeh has written about it quite a lot on [his blog](http://blog.ploeh.dk/2010/02/03/ServiceLocatorisanAnti-Pattern/), so I won't repeat him here. I strongly suggest reading his post.

Since it's very common pattern (:sad:), it will be covered here as well. After all, maybe it will fit your needs? Remember, [sometime square wheel work the best](https://en.wikipedia.org/wiki/Square_wheel).



There is a service that gives us instance of container. It's ~~`non-static`~~ injectable, but could been made `static` as well.

```csharp
public class IocContainerProvider { // a.k.a. Service Locator
  public Autofac.IContainer GetInstance() => BuildContainer();
  
  private IContainer BuildContainer() {
    var containerBuilder = new ContainerBuilder();
    
    containerBuilder
      .RegisterType<MssqlTenantDatabaseCreator>()
      .Named<ITenantDatabaseCreator>(nameof(MssqlTenantDatabaseCreator)) // C#6 feature 
      .AsSelf();
    
    containerBuilder
      .RegisterType<OracleTenantDatabaseCreator>()
      .Named<ITenantDatabaseCreator>(nameof(OracleTenantDatabaseCreator))
      .AsSelf();
    
    return containerBuilder.Build();
  }
}
```



And we have our `TenantDatabaseCreateService` into which we inject our `IocContainerProvider`.

```csharp
public class TenantDatabaseCreateService {
  private readonly IocContainerProvider _iocContainerProvider;
  
  public TenantDatabaseCreateService(IocContainerProvider iocContainerProvider) {
    _iocContainerProvider = iocContainerProvider;
  }
  
  public void CreateTenantDatabase(DatabaseType databaseType) {
    var creatorName = databaseType.ToString() + "TenantDatabaseCreator";
    var creator = _iocContainerProvider.GetInstance()
      				.ResolveNamed<ITenantDatabaseCreator>(creatorName);
    creator.Create();
    // Further instructions ...
  }
}
```


With `IocContainerProvider` in our class we gain access to initiate instance of **ANY** service registered in our IoC container. **That's probably  almost every type in our system!** That's a real [code smell](https://martinfowler.com/bliki/CodeSmell.html). 

> With great power comes great responsibility. ~ [Uncle Ben](https://www.unclebens.com/images/default-source/products/instant-brown.png?sfvrsn=2)

So yeah, Service Locator will do the work, but is highly discouraged.



## Resolving with an `IIndex<K,V>`

First of all

**This is the most suitable approach for this particular case**

[Autofac.Features.Indexed.IIndex](https://github.com/autofac/Autofac/blob/41044d7d1a4fa277c628021537d5a12016137c3b/src/Autofac/Features/Indexed/IIndex.cs) is Autofac feature which will provide us registered `ITenantDatabaseCreator` services . It acts like `Dictionary`. We can access our services by `[index]` of `DatabaseType` or do it in safe-way, with `TryGetValue` method.

So here we go. Register each implementation as `ITenantDatabaseCreator` with different `Key` .

```csharp
containerBuilder.RegisterType<MssqlTenantDatabaseCreator>().Keyed<ITenantDatabaseCreator>(DatabaseType.Mssql);
containerBuilder.RegisterType<OracleTenantDatabaseCreator>().Keyed<ITenantDatabaseCreator>(DatabaseType.Oracle);
```

Instead of injecting instance of `ITenantDatabaseCreator`, we will inject `Autofac.Features.Indexed.IIndex<DatabaseType, ITenantDatabaseCreator>` from Autofac library.

```csharp
public class TenantDatabaseCreateService {
  private readonly IIndex<DatabaseType, ITenantDatabaseCreator> _tenantDatabaseCreatorProvider;
  
  public TenantDatabaseCreateService(IIndex<DatabaseType, ITenantDatabaseCreator> tenantDatabaseCreatorProvider){
    _tenantDatabaseCreatorProvider = tenantDatabaseCreatorProvider;
  }
  
  public void CreateTenantDatabase(DatabaseType databaseType) { 
    var creator = _tenantDatabaseCreatorProvider[databaseType];
    creator.Create();
    // Further instructions ...
  }
}
```


Pretty neat, huh?

Let's briefly explain what's going on here.

We register all implementations as `ITenantDatabaseCreator`, each with different key. Then, we inject `IIndex<DatabaseType, ITenantDatabaseCreator>` to our service. It will resolve implementation basing on given `DatabaseType` parameter.

That's almost like in Delegate factory. What differs? **We didn't have to write this huge `switch` with all of supported implementations**. Autofac automatically generated this for us. This is great.

Since it works like `Dictionary`, only one implementation can be assigned to each `DatabaseType` and that's completely fine (or even desired!).

If you would like more information, check [Autofac documentation](http://docs.autofac.org/en/latest/advanced/keyed-services.html#resolving-with-an-index).

And don't forget to [register your services in correct scope](/.net/autofac/2016/10/06/Autofac-WebAPI-LifetimeScopes.html)!