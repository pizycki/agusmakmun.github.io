---
layout: post
title:  "Azure Logic Apps - parsing HTML"
date:  2017-08-04
tags: [Azure, LogicApp, RavenCage]
---

In this [post](http://izzydev.net/windowscontainers/docker/appveyor/ravencage/2017/04/26/Getting-your-Windows-Container-automatically-builded.html) I wrote about my pet project called [RavenCage](https://github.com/pizycki/ravencage), a conteinerization of pupular [NoSQL](https://www.google.pl/search?q=nosql) database [RavenDB](http://ravendb.net) in version [3.5](https://ravendb.net/docs/article-page/3.5/csharp/start/whats-new?page=1) for [Windows Containers](https://docs.microsoft.com/en-us/virtualization/windowscontainers). In short, I update [Dockerfile](https://github.com/pizycki/RavenCage-3.5/blob/master/Dockerfile) online on [GitHub](https://github.com/pizycki/RavenCage-3.5), create new [tag](https://help.github.com/articles/working-with-tags/)/[release ](https://help.github.com/articles/creating-releases/) and wait for [AppVeyor](https://ci.appveyor.com/project/pizycki/ravencage-3-5) do a build, test it and publish new images to [DockerHub](https://hub.docker.com/r/pizycki/ravendb/).

This process is pretty much automated except of one thing - new version check up.

One day I was curious if any new version has been released. No new build was released for over a month, so I was pretty sure that no work will be waiting there for me. And I was wrong. Two new builds were release over 5 days ago.

Well, I performed my publish-process described above, but I was not happy yet. I thought "it should be automated in some way too". I started looking for solution.

## Azure Logic Apps

In this [post](http://izzydev.net/edmxconverter/azurefunctions/.net/2017/06/20/Azure-Functions.html) I wrote a little about [Azure Functions](https://azure.microsoft.com/services/functions/). You can write and test them without Visual Studio or any other IDE. All is integrated within Azure. [Logic Apps](https://azure.microsoft.com/services/logic-apps/) are somewhat alike. What is different about them is that you don't write any code (not exactly). You just compose building blogs, simple `if` statements and loops. All that with components provided by different companies APIs (there is a lot of them!).

If you know [IFTTT](https://ifttt.com/) or [Zappier](https://zapier.com/) then Logic Apps are very similiar, but in my opinion, they allow to do a lot more.

[![Logic Apps demo video](http://img.youtube.com/vi/ksU5OCf3cn0/0.jpg)](http://www.youtube.com/watch?v=ksU5OCf3cn0)

But back to my problem. 

There are many ways to solve it, but Logic Apps seemed to be perfect for this.

## Program

There is a program that I came up with (in pseudo code).

```
Every day, exactly at 23:55 UTC, do:
  - Get source code of 'RavenDB 3.5 Release Notes' page
  - Look up for today date (in correct format)
    - if it is present
      - Create a new task with reminder in my specific Wunderlist list
```

### Schedule execution
There are some kind of triggers for our workflow: . I choosed **Recurrence** component which is simple [Cron](https://en.wikipedia.org/wiki/Cron) task. It simply runs in intervals.

> Every task can has timeout and retry option.

I set mine to run every day at 23:55 UTC. We're running in cloud so timezone matters! (and when don't they...)

![Set cron task]({{site.url}}/static/img/posts/Azure-Logic-Apps-parsing-HTML/recurrence.png)

### Get HTML and parse it

Getting webpage was easy, I used **Http Request** component. As an input it accept HTTP method, URI and other HTTP request related stuff that I didn't need.

![Get source of RavenDB release notes]({{site.url}}/static/img/posts/Azure-Logic-Apps-parsing-HTML/http.png)

The output of HTTP request is (how obvious) HTTP response. It contains properties like Body, Status. That HTTP Response is an object that can be passed thourgh later components. It stays in the context for whole workflow lifetime.

Before getting into response from RavenDB website, we need actual date. We can create variable of specific type (`string`) and assaign value to it. This is where [Workflow Definition Language](https://docs.microsoft.com/en-us/azure/logic-apps/logic-apps-workflow-definition-language) comes handy.

So we create `today date` variable with the value returned by `utcNow()` function. Notice that we can format the date passing  `format` argument (right side of screenshot below).

![Create variable with today date in specific format]({{site.url}}/static/img/posts/Azure-Logic-Apps-parsing-HTML/variable.png)

As we have HTML and today date, we proceed to look up for specific string in our webpage.

At first, I wanted to use a Regex component, but unfortunetly nothing like this existed so far. I've tried with creating Azure Function to parse html for me, but quickly dropped the idea because of increase of complexity which was completly unnecessary. 

I even created [Stackoverflow question](https://stackoverflow.com/questions/45456381/parse-text-in-azure-logic-apps) for this.

After some playing around I finally got what I wanted. I created **If condition** component which check if received HTML contains `today date`. Simple!

![Add IF statement]({{site.url}}/static/img/posts/Azure-Logic-Apps-parsing-HTML/if.png)

Unfortently, I don't see posibility for any more complex parsing text with only use of WDL right now.

### React on new RavenDB

So, our workflow hits the "new RavenDB version detected" condition. What now?

We could create a new issue on our GitHub repository for new image publish. Not a bad idea for popular repository which we check up on everyday. But the RavenCage is not the one.

So instead of creating GitHub Issue, we will create a new task on Wunderlist with appropriate reminder.

Again, we will just use ready-to-select components, prepared by Microsoft. Yeah, since Wunderlist has been acquired by MS, we can meet it more often in MS solutions. Well, I don't mind, I've been using Wunderlist for a very long time and I like it for its simplicity.

![Creating Wunderlist task]({{site.url}}/static/img/posts/Azure-Logic-Apps-parsing-HTML/wunderlist.png)

> A little bit odd and frustrating is that **Create a task** component creates **two** tasks actualy. It's a bug and I hope it will be fixed.

As you can see, **Set a reminder** uses and `id` returned by **Create a task**.

*I wonder how hard would it be extend those components...*



## All together

Within a day I've managed to get familiar with new framework and create productive tool. Yay!

The final version of my workflow looks like this

![]({{site.url}}/static/img/posts/Azure-Logic-Apps-parsing-HTML/workflow.png)

If you want moar, you can go into full JSON mode and tweak your workflow without UI limitations.

So, for sake of completness here is the above flow in JSON

```json
{
    "$connections": {
        "value": {
            "wunderlist": {
                "connectionId": "/subscriptions/.../providers/Microsoft.Web/connections/wunderlist",
                "connectionName": "wunderlist",
                "id": "/subscriptions/.../providers/Microsoft.Web/locations/northeurope/managedApis/wunderlist"
            }
        }
    },
    "definition": {
        "$schema": "https://schema.management.azure.com/providers/Microsoft.Logic/schemas/2016-06-01/workflowdefinition.json#",
        "actions": {
            "Condition": {
                "actions": {
                    "Create_a_task": {
                        "inputs": {
                            "body": {
                                "completed": false,
                                "list_id": 000000000,
                                "starred": true,
                                "title": "@{variables('today date')}"
                            },
                            "host": {
                                "connection": {
                                    "name": "@parameters('$connections')['wunderlist']['connectionId']"
                                }
                            },
                            "method": "post",
                            "path": "/tasks",
                            "retryPolicy": {
                                "type": "none"
                            }
                        },
                        "limit": {
                            "timeout": "PT20S"
                        },
                        "runAfter": {},
                        "type": "ApiConnection"
                    },
                    "Set_a_reminder": {
                        "inputs": {
                            "body": {
                                "date": "@{addHours(utcNow(), 3)}",
                                "list_id": 000000,
                                "task_id": "@body('Create_a_task')?.id"
                            },
                            "host": {
                                "connection": {
                                    "name": "@parameters('$connections')['wunderlist']['connectionId']"
                                }
                            },
                            "method": "post",
                            "path": "/reminders",
                            "retryPolicy": {
                                "type": "none"
                            }
                        },
                        "limit": {
                            "timeout": "PT20S"
                        },
                        "runAfter": {
                            "Create_a_task": [
                                "Succeeded"
                            ]
                        },
                        "type": "ApiConnection"
                    }
                },
                "expression": "@contains(body('HTTP'), variables('today date'))",
                "runAfter": {
                    "Initialize_variable": [
                        "Succeeded"
                    ]
                },
                "type": "If"
            },
            "HTTP": {
                "inputs": {
                    "method": "GET",
                    "uri": "..."
                },
                "runAfter": {},
                "type": "Http"
            },
            "Initialize_variable": {
                "inputs": {
                    "variables": [
                        {
                            "name": "today date",
                            "type": "String",
                            "value": "@{utcNow('yyyy/MM/dd')}"
                        }
                    ]
                },
                "runAfter": {
                    "HTTP": [
                        "Succeeded"
                    ]
                },
                "type": "InitializeVariable"
            }
        },
        "contentVersion": "1.0.0.0",
        "outputs": {},
        "parameters": {
            "$connections": {
                "defaultValue": {},
                "type": "Object"
            }
        },
        "triggers": {
            "Recurrence": {
                "recurrence": {
                    "frequency": "Day",
                    "interval": 1,
                    "startTime": "2017-08-01T23:55:00Z",
                    "timeZone": "UTC"
                },
                "type": "Recurrence"
            }
        }
    }
}
```
Hope you enjoyed it!