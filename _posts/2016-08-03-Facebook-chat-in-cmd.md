---
layout: post
title:  "Facebook chat in cmd"
date:   2016-08-03
categories: [.NET, Edge.js, Facebook]
---

Install [NPM package manager](https://www.npmjs.com/)
```
choco install nodejs npm -y
```

Create `Console Application`. 

Install [Edge.js](https://github.com/tjanczuk/edge) with [NuGet](https://www.nuget.org/packages/Edge.js/).
```
Install-Package Edge.js
```

Open `cmd` in root of your Console app and install [facebook-chat-api module](https://github.com/Schmavery/facebook-chat-api)
```
npm install facebook-chat-api
```

Then insert snippet below for messaging you on FB by running application.

```csharp
using System;
using System.Threading.Tasks;
using EdgeJs;

namespace ConsoleApplication1
{
    class Program
    {
        public static async Task Start()
        {
            var func = Edge.Func(@"

var login = require('facebook-chat-api');

return function (data, cb) {
    login({email: data.email, password: data.password}, function callback (err, api) {
        if(err) return console.error(err);
        api.sendMessage(data.body, data.thread);
    });

    cb();
}

        ");

            Console.WriteLine(await func(new
            {
                email = "XXXXXXXXXX",
                password = "XXXXXXXX",
                body = "blabla",
                thread = "100000548414228" // Tip: to find your own ID, you can look inside the cookies. The userID is under the name `c_user`
            }));
        }

        static void Main(string[] args)
        {
            Start().Wait();
            Console.ReadKey();
        }
    }
}
```

You can explore [examples](https://github.com/Schmavery/facebook-chat-api/blob/master/DOCS.md#getUserID) for more cool usages.

---
_Image:_ [Iris Classon Blog](http://irisclasson.com/2014/06/12/video-learn-edge-js-part-1-what-is-it/)