---
layout: post
title:  "Cake and WebDeploy"
date:   2016-08-03
categories: [.NET, Cake, WebDeploy]
---

[Cake](http://cakebuild.net/) is great open source alternative for building your **.NET** solutions. What is more, it allows to attach ~~your own written~~ downloaded from [nuget](https://www.nuget.org/packages?q=cake)/[GitHub](https://github.com/search?l=C%23&q=cake&type=Repositories&utf8=%E2%9C%93) plugins (a.k.a. [_Addins_](http://cakebuild.net/addins)) which will do the job for you.

For example, as a .NET web developer I often make deploys to the dev/test/rc/prod environment. And you know well that repetitive works is the worst. So why don't we automate this and let it be done just itself?

Get familiar with [basics of Cake](http://cakebuild.net/docs/tutorials/getting-started) and clone [example repo](https://github.com/cake-build/example). It contains example build script which might come very helpful while building your own one. Just like we're gonna use it right now.

I highly recomend installing [Visual Studio Code](https://code.visualstudio.com/) and [suitable extension](https://marketplace.visualstudio.com/items?itemName=cake-build.cake-vscode) for setting up Cake script. You can [install VSC with Chocolatey](https://chocolatey.org/packages/VisualStudioCode).

We're going to add new `Task` called `Deploy`. To achieve that, we're using [Cake.WebDeploy
](https://github.com/SharpeRAD/Cake.WebDeploy) addin.


```csharp

// All addins will be downloaded from nuget repository by cake bootstrapper
#addin "Cake.WebDeploy"

...

Task("Deploy")
    .Description("Deploy to a remote computer with web deployment agent installed")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() =>
    {
        DeployWebsite(new DeploySettings()
        {
            SourcePath = "./src/Example.Web/bin",
            SiteName = "CakeTest",

            ComputerName = "192.168.225.130",
            Username = "WDeployAdmin",
            Password = "WDeployAdmin_PWD"
        });
    });
```

To call our task, simply open cmd/terminal (like [cmder](https://chocolatey.org/packages?q=cmder)), change directory to one with `cake.ps1` (in the `root` of your repo) and call 

```
cake.ps1 -Target Deploy
```

Watch as magic is going to happen.

If we'd like to manualy Zip and transfer our website i.e. via FTP, we can use another `Task`.

```csharp
Task("ZipWebsite")
    .IsDependentOn("Run-Unit-Tests")
    .Does(() => 
    {
        Zip("./src/Example.Web/bin", "./src/Example.Web/Package.zip");
    });
```
It will pack directory with compiled project into `zip` file.

