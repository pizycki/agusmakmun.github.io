---
layout: post
title:  "Azure Functions"
date:  2017-06-20
categories: [EdmxConverter, AzureFunctions, .NET]
---

**TL;DR:** Azure Functions is Microsoft implementation of serverless nanoservices. Best way to start using them is to install Visual Studio Code and CLI. For manually executing HTTP requests use Postman, but for longer run use test framework (like xUnit).

## Continous learning

I like the idea of picking a new language/framework/platform/whatever and learning it for one year. We're IT guys. Our industry grows with enormous speed and forces us to continuous knowing new things. Ignoring them is no good for **any of us**.

Back in 2016, my new platform was [Docker](https://www.docker.com), specially [Windows Containers](https://docs.microsoft.com/en-us/virtualization/windowscontainers/about/). As a subject to work on and learn Docker in practice, I have had developed open source project which goal was to containerize [RavenDB](http://https://ravendb.net). The project turned out to be successful and [RavenCage 3.5 images](https://github.com/pizycki/RavenCage-3.5) are still available on [DockerHub](https://hub.docker.com/r/pizycki/ravendb/). I've also been issuing [images for RavenDB 4.0](https://github.com/pizycki/RavenCage-4.0), but dropped that after [official channel has been announced](https://ayende.com/blog/178049/ravendb-4-0-on-docker). I guess that's because people were using images prepared by me, not Raven team member, and the image quality couldn't have been assured by the Raven team. That's sad, but true, and I fully understand it. Thankfully, as I've heard from one of Raven team members that there are no plans for releasing images of RavenDB 3.5, so there is still job for me :)

On 2016/2017, I've started hearing more about [AWS Lambda](http://docs.aws.amazon.com/lambda/latest/dg/welcome.html) and [Azure Functions](https://azure.microsoft.com/en-us/services/functions/). Generally, I'm more Microsoft guy and I do have some experience with Azure, so I started looking forward the second option.

## Azure Functions

"But what are those?" you'd ask. 

Well, after [microservices](https://martinfowler.com/microservices/) boom, the time has come to make things even smaller. Here I present you **nanoservices** and **serverless**.

**Nanoservices** are just smaller microservices. Microservice is oriented on single, encapsulated business domain (users, comments, authorization, etc.). Nanoservice is oriented on every action that you could do in microservice. 

Good examples of nanoservices are: Create user, Send verification email, Verify user. Each nanoservice can be *triggered* on its own way. "Create user" can be triggered by HTTP endpoint (ie. POST request) and "Send email" can be triggered by new message in storage queue.

In serverless approach its all about not caring about the server. That's it. You code your program and upload it to the cloud. The platform will take care of deployment and all dependencies required by your service to work.

> There are tons of blog-posts and articles about serverless and nanoservices and there is no point write about it here again. Check out links in the end of the post if you want some more.

What is worth to mention, both Azure Functions and AWS Lambda, heavily relay on containers. In fact, they're containers, but decorated with nice API. An instance of nanoservice is hosted by a single container. There are some limitations followed by this. If function has not been used for about 10 minutes, the hosting container is stopped (and destroyed). To spin up a new instance of container there must be small period of time. That leads sometimes to unresponsive API (ie. time outs). Fortunately, there are some techniques keeping your container alive (like simple pinging).

In Azure Functions you can pick any .NET language you want (plus JavaScript, it's Azure after all and it's commonly known that Azure ♥ JS). As I didn't want to struggle with F# or JS too much, I've choosed C#, my main programming language. It's always a little bit easier knowing new things with something you already know. Actually it's [scriptCs](http://scriptcs.net), so the syntax is almost the same as Microsoft implementation.

After having little fun with Azure Functions, I started thinking where I could use the new, shiny technology.

And an idea came up. Not long time ago, I had to desterilize [Entity Framework context](https://stackoverflow.com/a/18645132/864968) to see model generated during Code First migration. It turned out that there is no online tool for such task.

Easy to implement, fits technology, might be quite extendible... let's do this!

I made a simple proof of concept. Function with HTTP trigger that accepts only POST requests. Nothing special. Input-Output. I was sending serialized EF model (a.k.a. [edmx](https://stackoverflow.com/questions/4941892/what-is-the-purpose-of-edmx-files)) and expecting my function to respond with valid XML.

I already knew scriptCs before, so C# with no Intelisense didn't really hit me. Yet, I started to wonder about some things. My first impressions were like "how can one write software this way?"

* You can't commit your code to the source control;
* You can't attach debugger to the function you are working on;
* You can't run unit tests against your functions

Azure development team assured that they were already working on it.

And they were.

## Lanching Azure Functions locally

### Azure Function Visual Studio Tools

In late 2017, [Azure Function tooling for Visual Studio](https://blogs.msdn.microsoft.com/webdev/2016/12/01/visual-studio-tools-for-azure-functions/) has been released. 

It has given the developer ability to create Azure Function in VS solution as project. The structure of project was really very similar what was in the browser, so I simply copy-pasted all my files from Azure to newly created project. 

To run function in debug mode just hit F5 in VS. Simple.

But there is another way...

### Visual Studio Code Tooling

What I liked more was setting up local env with [VS Code](https://code.visualstudio.com).

To do that, you need to install [Azure Functions CLI](https://github.com/Azure/azure-functions-cli) with [npm](https://www.npmjs.com).

```bash
npm install -g azure-functions-cli
```

You should check out [scriptCs plugins for VS code](https://marketplace.visualstudio.com/search?term=scriptcs&target=VSCode&category=All%20categories&sortBy=Relevance) too!

The simplest way to run your function is opening terminal (i.e. inside VS Code), go to the root directory of your project and run:

```bash
func host start
```

If all goes fine, you will get list of running functions on your local machine with their endpoint URLs.

## Testing

To execute your function you can either use browser (for GET requests), `azure-functions-cli` or any other HTTP request tooling. I prefer using [Postman](https://www.getpostman.com).

With postman you can not only run your requests against HTTP API. You can also [write tests inside the application](https://www.getpostman.com/docs/postman/scripts/test_scripts) and run them all at once assuring that all functions respond in correct way. Later, [you can export your requests collection]([https://www.getpostman.com/docs/postman/scripts/test_scripts](https://www.getpostman.com/docs/postman/scripts/test_scripts)) and, for example, store it in your source control. 

More here: [Postman test scripts](https://www.getpostman.com/docs/postman/scripts/test_scripts)

> Remember though, your local env will never be the cloud.

### Integration tests in xUnit as library

Another way of testing your functions would be creating project with "unit" tests, as I did. The advantages over using Postman is that you can automate it easier and avoid messing around with postman request collections (which is quite harsh if you don't pay for Pro version).

I've created .NET Core project (because I work in VSCode) and downloaded xUnit. I simply followed [this guide](http://xunit.github.io/docs/getting-started-dotnet-core). Then I added small piece of tests. Here is the example.

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace EdmxConverterAF_Tests
{
    public class ConvertEdmxFromBase64ToXml : IClassFixture<EdmxConverterApiConverter>
    {
        EdmxConverterApiConverter _edmxApi;

        public ConvertEdmxFromBase64ToXml(EdmxConverterApiConverter edmxApi)
        {
            _edmxApi = edmxApi;
        }

        [Fact]
        public async Task convert_valid_base64_edmx_to_xml()
        {
            // Arrange & Act
            var model = "H4sIAAAAAAAEAO19224cOdLm/Q...wfPniTBAA="; // Shortened
            var response = await _edmxApi.ConvertBase64ToXml(model);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            const string expcted = "&lt;?xml version=\"1.0\" encoding=\"utf-8\"?&gt;\r\n&lt;Edmx Version=\"3.0\"..."; // Shortened
            var actual = await response.Content.ReadAsStringAsync();
            Assert.Equal(expcted, actual);
        }
    }

    public class EdmxConverterApiConverter
    {
        public string ApiUrl { get; set; } = "https://edmx.azurewebsites.net";

        public async Task<HttpResponseMessage> ConvertBase64ToXml(string base64)
        {
            using (var client = CreateHttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "/api/decompress")
                {
                    Content = new StringContent(base64, Encoding.UTF8, "text/plain")
                };
                return await client.SendAsync(request);
            }
        }

        private HttpClient CreateHttpClient()
        {
            return new HttpClient()
            {
                BaseAddress = new Uri(ApiUrl),
            };
        }
    }
}
```

As you can see I invoke HTTP request against real Azure Function endpoint. We should be talking about integration tests right now, not unit tests.

Remember that xUnit run tests by default  in parallel and it probably should be changed in configuration. More can be found in [xUnit Parallelism documentation](https://xunit.github.io/docs/running-tests-in-parallel.html).

As a test helper we use `EdmxConverterApiConverter`. This fixture is our interface to connect to our Azure Functions. It should contain as many methods as we have functions with HTTP endpoints or consider separate fixture for every function. Just to keep it clean.

> It should be avoided to create multiple `HttpClient` instances at once as it is followed by opening many TCP/IP sockets. Such operation is very expensive and should be replaced with pool of available `HttpClient` ready to be used without need of destroying each one after every HTTP request. 

With such test library we are able to modify and run our tests locally, but checking real Azure Functions. This approach is more flexible and solid (IMHO). We can store our code in Git, so we won't loose it in future and also we will keep its modification history. You can also run such tests on build server which is VERY nice feature.

So, that's how we can keep our Azure Functions covered with tests.

## What about the interface?

Whole HTTP API can be done with Azure Functions. Yet, I still need some client which will help user calling it. In my current work I have to deal with Angular 1.4x. I've once touched Aurelia for one student project and it was quite cool. So how about merging those two? Component approach and Angular popularity? I've choosed Angular 2. But this is topic for another blog post. Probably pt. 2.

## OpenAPI

Looks like I've missed something in my research. I just found an [OpenAPI](https://docs.microsoft.com/en-us/azure/azure-functions/functions-api-definition-getting-started) available for  Azure Functions. It's currently in preview for Azure Functions, but even now it looks promising.

I'll try use it in my EdmxConverter.

---

- [Unit Testing Azure Functions and .csx Files](https://stackoverflow.com/questions/42513577/unit-testing-azure-functions-and-csx-files)

- [VS tools for Azure Functions](https://blogs.msdn.microsoft.com/webdev/2016/12/01/visual-studio-tools-for-azure-functions/)

- [Serverless patterns](https://serverless.com/blog/serverless-architecture-code-patterns)

- [Testing with Postman](https://www.getpostman.com/docs/postman/scripts/test_scripts)

- [Postman request collections](https://www.getpostman.com/docs/postman/collections/creating_collections)

- [Automated testing in Postman](http://blog.getpostman.com/2014/03/07/writing-automated-tests-for-apis-using-postman/)

- [Serverless patterns](https://github.com/yochay/serverlesspatterns)

- [Call other functions inside azure functions](http://devslice.net/2016/09/azure-functions-call-functions/)

- [Reusing code in Azure Functions](http://devslice.net/2016/08/azure-functions-reusing-code/)

- [Strategies for testing your code in Azure Functions](https://docs.microsoft.com/en-us/azure/azure-functions/functions-test-a-function)

- [Running Azure Functions Locally with the CLI and VS Code](https://blogs.msdn.microsoft.com/appserviceteam/2016/12/01/running-azure-functions-locally-with-the-cli/)

- [Azure/azure-functions-cli](https://github.com/Azure/azure-functions-cli)

- [Code and test Azure functions locally](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local)