---
layout: post
title:  "Autofac, API and LifetimeScopes"
date:   2016-10-06
categories: [.NET, Autofac]
---
Once again I came across problem of registering _services_ in Autofac container in API oriented project.

Basically it's registering business-logic-services to be resolved as instance per (API) request. It works... until you'd like to resolve your service in Integration tests or Hangfire/Quartz job.

The whole problem is discussed on [StackOverflow question](http://stackoverflow.com/questions/12127000/autofac-instanceperhttprequest-vs-instanceperlifetimescope) and in this [article](http://decompile.it/blog/2014/03/13/webapi-autofac-lifetime-scopes/#more-620).

Two most important things to be remarkable of are:

> Use **InstancePerLifetimeScope** if you want your dependency to be resolvable from **any scope**. Your dependency will be disposed of when the lifetime is disposed. In the case of the root scope, this will not be until the application terminates.

and

> Use **InstancePerApiRequest/InstancePerHttpRequest** if your dependency should only be resolvable from the **context of an HTTP/API request**. Your dependency will be disposed of at the conclusion of that request.

Under the hood, `InstancePerRequest` is just a simple `InstancePerMatchingLifetimeScope` with specific tag.
You can check this by yourself [on GitHub, in Autofac source code](https://github.com/autofac/Autofac/blob/41044d7d1a4fa277c628021537d5a12016137c3b/src/Autofac/RegistrationExtensions.cs#L1401).

Convention test that checks all registration instances in Autofac container to be not registered as `InstancePerRequest` is shown below.

```csharp
public class AutofacRegistration_Tests
{
    [Fact]
    public void any_type_in_service_layer_should_NOT_be_registered_as_InstancePerRequest()
    {
        var containerRegistrations = GetContainer().ComponentRegistry.Registrations;
        var results = containerRegistrations.ToDictionary(reg => reg, IsRegistrationScopeInstancePerRequest);

        /* When test fails, debug this test and run following command in `Immediate console`: 
         * var invalid = results.Where(r=>r.Value);
         * Then look into your `Local variables` window for new variable named "invalid".
         * It contains all invalid registrations.
         */

#if DEBUG
        foreach (var registration in results.Where(result => result.Value).Select(result => result.Key))
        {
            Debug.WriteLine($"Type {registration.Activator} is registered with invalid scope.");
        }
#endif

        results
            .Where(result => result.Value)
            .Select(result => result.Key)
            .ShouldBeEmpty();
    }

    private static bool IsRegistrationScopeInstancePerRequest(IComponentRegistration registration)
    {
        if (registration.Lifetime.GetType() != typeof(MatchingScopeLifetime))
            return false;

        // ReSharper disable once PossibleNullReferenceException
        var tags = typeof(MatchingScopeLifetime)
            .GetField("_tagsToMatch", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(registration.Lifetime) as object[];

        return tags != null && tags.Any(tag => tag == MatchingScopeLifetimeTags.RequestLifetimeScopeTag);
    }

    private IContainer GetContainer()
    {
        ...
    }
}
```