---
layout: post
title:  "Language-Ext real example"
date:   2018-07-30
tags: [.NET, FP, LangExt]
---

[Language-ext](https://github.com/louthy/language-ext) is the biggest library to write [functional code](https://en.wikipedia.org/wiki/Functional_programming) in C#. 

To start writing in Lang-ext you need to posses **a lot** of know-how. The [Readme](https://github.com/louthy/language-ext/blob/master/README.md) file is great, the [wiki](https://github.com/louthy/language-ext/wiki) is cool, but must of the missing parts are shredded among [GitHub](https://github.com/louthy/language-ext) issues and [Gitter](https://gitter.im/louthy/language-ext?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) chat. You just gotta find them , but it might not be an easy task.

These links are specially interesting

https://github.com/louthy/language-ext/issues/313

(I’ll add some more later)



Okay, let’s see how it looks like.

I’m going to present one, but the most complex and comprehensive example I’ve prepared at my work while we were looking for library to deal with `nulls` in greenfield project.

The example is simple (but real) invocation of payment start. To do so, we need payment request and some additional data (sent in HTTP request headers).

Ok, the example.

Here are some methods we‘d like to use in our example. `StartPayment` and `StartPaymentResponse` are my classes and `ValidationFailure` is from FluentValidation library. The rest is from Lang-ext.

```csharp
Validation<ValidationFailure, StartPaymentRequest> Validate(StartPaymentRequest request) =>
    Validation<ValidationFailure, StartPaymentRequest>.Success(request);
Validation<ValidationFailure, string> Validate(string apiKey) => 
    Validation<ValidationFailure, string>.Success(apiKey);
Option<string> GetApiKey() => Some("Some api key");
Option<string> GetAppCode(string apiKey) => Some("Some app code");
Result<StartPaymentResult> StartPayment(StartPaymentRequest request, string appCode) => 
    new Result<StartPaymentResult>(new StartPaymentResult());
Task<Result<StartPaymentResult>> StartPaymentAsync(StartPaymentRequest request, string appCode) =>
    Task.FromResult(new Result<StartPaymentResult>(new StartPaymentResult() { }));
```

And here is what our application might look like. Image it’s some kind of service or controller action.

```csharp
var request = new StartPaymentRequest();

/* This will create validation on Option<string>. ApiKey will be valid only when present (in Some state). */
var validateApiKey = GetApiKey().ToValidation(new ValidationFailure("ApiKey", "API Key is missing in headers"));

Validation<ValidationFailure, string> v = 
    from v1 in validateApiKey
    from v2 in Validate(request)
    select v1 + v2;

/* Not sure what happens to ValidationFailure from second validation... */

/* Alternativly, we can do this way */
//var v_alt = (apiKey, Validate(request)).Apply((_, req) => req);

/* C# evaluates functions one-by-one. If one failes, the rest is omitted as we haven't got all required variables (v1 and v2) to complete the last statement */

/* All of the above is the great use example of monad being aplicative structure */

Option<string> appCode = 
    from key in v.ToOption()
    from code in GetAppCode(key)
    select code;

/* Here we want to use wrapped object with Optional<> type as the argument.
 * The methods accepts string so we cannot simply pass the ApiKey.
 * The whole operation would only succeed when ApiKey is present (valid) and there is existing AppCode assosiated with AppCode.
 * Otherwise we'd get None.
 */

/* Have you noticed Railiway Oriented Programing approach here so far? */

Option<Result<StartPaymentResult>> mPaymentResult = 
    (from code in appCode.ToTryOption()
     from req in TryOption(request)
     select StartPayment(req, code)).ToOption();

/* This one is a bit tricky and requires bigger familiriaty with types provided by Lang-ext.
 * TryOption wraps **some operation** which is not quite popular in OOP.
 * But we are in FP world right now.
 * We transform AppCode from Optional to function which creates our AppCode.
 * Then we do the same thing for request.
 * We apply Both AppCode and Request on StartPayment which will return us a TryOption<Result<StartPaymentResult>>.
 * Yeah, it doesn't look well, but kind of makes sense.
 */

string result = match(mPaymentResult,
    Some: mResult => mResult.Match(
       Succ: res => $"Success! Payment ID = {res.PaymentId}",
       Fail: ex => ex.Message),
    None: () => "Invalid requqest.");

/* In the end we have to perform some pattern matching 
 * There are basicly 3 states in our case.
 * 1. Some>Succ: when result completes successfuly;
 * 2. Some>Fail: when result completes with failure;
 * 3. None: when the operation has not been even invoked
 * This explaines the TryOption<Result<StartPaymentResult>> pretty match.
 */

result.Should().Contain("42");
```

The example above is a sync method.

We can also do things with `async/await`.

For comparison, I’ve deleted all comments to see how much descriptive the code is itself.

```csharp
var request = new StartPaymentRequest();

var validateApiKey = GetApiKey().ToValidation(new ValidationFailure("ApiKey", "API Key is missing in headers"));

var v = from v1 in validateApiKey
        from v2 in Validate(request)
        select v1 + v2;

var appCode = from key in v.ToOption()
              from code in GetAppCode(key)
              select code;

TryAsync<Result<StartPaymentResult>> mPaymentResult =
    from req in TryAsync(request)
    from code in appCode.ToTryAsync()
    select StartPaymentAsync(req, code);

string message = await mPaymentResult.Match(
    Succ: tryRes => tryRes.Match(
        Succ: payRes => $"Payment succeeded! PaymentID = {payRes.PaymentId}",
        Fail: ex => ex.Message),
    Fail: ex => ex.Message);

message.Should().Contain("42");
```

It’s not that bad to be honest.

What we can achieve with such approach

1. Better declaritivity
2. Fewer places for `Null-Ref-Exception` (they’re still there unfortunately)
3. ROP
4. No-one will understand our code

Yes, that’s sad but true. Lang-ext forces us to use C# feature in very uncommon way. Have you ever thought about using Linq to write statements in your program? (sic!) With awkward syntax comes feeling that we’re fighting the compiler.

It doesn’t mean you can’t write awesome things with Lang-ext. Sure you can, but wouldn't it be easier to just shift to F#? There is everything already there without awkward type inference errors.
