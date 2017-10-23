# How to get into Functional Programming

1. what is FP
2. WTF#
   1. immutability !!!
3. Funkcyjne zabawki w obiektowej piaskownicy
4. Monad club
   1. linq is monad?
5. lang-ext - WOW
   1. null handling
   2. pattern matching
   3. C# 6
6. unit testing
   1. static can be cool
   2. mock hell
7. elixiry, haskelle, cloujure... i znow fsharpforfunandprofit
8. EdmxConv
9. azure functions
   1. too edgy
10. Monacs
11. railway pattern
12. lang-ext in practice
    1. linq = overkill
13. khirkov
14. C# FP orange book
15. Quora
    1. every day new article
    2. Bartosz milewski
16. Category Theory
17. ​




## What is FP

First we have to figure out what functional programming really is and how can benefit from it. 

This [Quora answer](https://www.quora.com/What-is-functional-programming) describes FP quite well.

To see FP in examples, check out blog post with [practical examples in Python](https://maryrosecook.com/blog/post/a-practical-introduction-to-functional-programming). Don't worry if you don't [speak python](http://i.imgur.com/KGrV41o.png), examples are really easy to understand for anyone. Some of the examples might seem quite similar to [https://en.wikipedia.org/wiki/Big_data](https://en.wikipedia.org/wiki/Big_data) approach (like: [MapReduce](https://en.wikipedia.org/wiki/MapReduce)). This is not coincidence as FP is a great tool for processing (large/any samples) data.



## C# and FP

C# is object-first language, but since version 2.0, functional-related features are introduced to every new release. One of the biggest and the most beneficial functional features that made C# so loved was introducing LINQ. Almost whole LINQ is designed on functional principles.

We'll come back to LINQ again in this article as it's really interesting.

Generics were ported from F# to C# almost 1:1. [Don Syme](https://twitter.com/dsyme) did this. He's one of the creators of F#.

### language-ext

[language-ext](https://github.com/louthy/language-ext) is the top1 when asking Google about "[C# functional](https://www.google.pl/search?q=C%23+functional)".

It's main puropose was to enable C# programmers to model their code in more functional manner.

#### Great documentation

lang-ext has one of the most impressive documentations I've seen in open source projects. Reading only ReadMe file is so interesting and full of good advices that every developer should read it at least once. I guess, I did that more than 5. 

There is also whole [Wiki](https://github.com/louthy/language-ext/wiki) which is translated version of [Thinking Functionally](https://fsharpforfunandprofit.com/series/thinking-functionally.html) to C# examples. Truly great work.

#### Code

Reading Readme is full of exaples how to use the framework. It contains many `Left|Right` variations.

```csharp
// None is functional non-nullable answer to null problem in OOP.
OperationThatMightReturnNone(
	Some => "This is not null, so everything is fine. We can continue.", 
	None => "OMG, no value was returned, I should act to this. Let's show error.");
```

In this example `Left` is acting when returned value is not None, and `Right` is None. This might seem to be very similiar to `if` statement. Ok, for now we can agree, but as you know FP better, you'll see that this is very sexy way to control the program flow.

There are also other variants of `Left|Right` like `Success|Fail` for operations, `Valid|Invalid` for validation. There is nothing against adding your own... monad!

> The example is only concept. I'm not sure if any applications would have such a code. But it shows the case pretty clearly. 

There are many more in the lib, but I felt that `Left|Right` representation of monads are the most crucial.

#### Community

The people are one of the most important factors when talking about frameworks. In case of lang-ext I can say that, it's quite good. It's not very popular (as C# and FP don't go that well as F# and FP), but surely you'll find someone who will help. The community grows and the gitter channel is the great way for your newbie questions.

#### Downsides

Unfortunately the learning curve of this project is quite steep. To fully get fluent in using lang-ext you'll need a lot of time and practice. There're no shortcuts.

Because of uncommon use of LINQ queries, this project is one of the most demanding for Visual Studio error checker to process. REALLY. Medium and large projects might suffer from laggy VS. Just check out this [issue](https://github.com/louthy/language-ext/issues/249) where VS team member offered to help.

#### Not for me

Unfortunately I was not determinated to get learn the *good way* of using lang-ext, I started looking for something easier and lighter.

### Functional Extensions for C##

After watching  [course on Pluralsight](https://app.pluralsight.com/library/courses/csharp-applying-functional-principles/table-of-contents), I tried using presented there library called simply [CSharpFunctionalExtensions](https://github.com/vkhorikov/CSharpFunctionalExtensions). It was really lightweight corresponding to lang-ext. Some types, some extensions and that's it.

The main concept is based on building expressions with few building blocks: `OnSuccess`, `OnError` and `Map`. The are more blocks, but the really crucial are those three. Here's an example taken from Readme.

```csharp
return _customerRepository.GetById(id)
    .ToResult("Customer with such Id is not found: " + id)
    .Ensure(customer => customer.CanBePromoted(), 
            	"The customer has the highest status possible")
    .OnSuccess(customer => customer.Promote())
    .OnSuccess(customer => 
               	_emailGateway.SendPromotionNotification(customer.PrimaryEmail, customer.Status))
    .OnBoth(result => result.IsSuccess ? Ok() : Error(result.Error));
```

What's great about that approach is that you can easily extend by adding your own blocks. Here is one of mine, which wraps some code with `try-catch` statement.

```csharp
// A -> K
public static class OnSuccessTryExtensions {
  public static Result<K> OnSuccessTry<T, K, E>(
    this Result<T> result,
    Func<T, Result<K>> func, string error = "") where E : Exception {
    
      if (result.IsFailure)
          return Result.Fail<K>(result.Error);
    
      try {
          return func(result.Value);
      }
      catch (E e) {
          return Result.Fail<K>(error == string.Empty ? e.Message : error);
      }
  }
```

With help of this lib I created [EdmxConverter](https://github.com/pizycki/EdmxConverter-Server) which, I guess, can be good example of program written in C# in functional way. 

If I'm wrong, tell me ;)

#### Other things

Beside it's lightweightness and ease-of-use there some things worth to point out.

First is that this library is nothing robust. It's great for beginners or for people how want to write less error-prone programs. But it's functional abilities are very limited. Basically it's all only about flow control. But don't forget that C# itself is getting more and more functional-ish.

As long you don't need them, you'll be happy.

### Monacs

[Monacs](https://github.com/bartsokol/Monacs) is very similar to "Functional Extensions for C#". It is a library created by [Bart Sokol](https://twitter.com/bartsokol) and his coworkers and it was presented in his talk [Functional developer in object oriented world](https://www.youtube.com/watch?v=BmTJaYkjWAg&list=PLIaOVSy19z6bppj6oxQRMCFOZ5dDkiUA5&index=13) [PL].

It's in very early stage of development, so I won't say anything more here about it, but it looks like a good alternative. I haven't used it though. Maybe you can give it a try ;)

###Monad Club

A [Monad club](https://www.youtube.com/watch?v=ghkcIr_Zr1g) is a talk (in polish) of [Marcin Malinowski](https://twitter.com/orientman) about monads presented on LINQ examples.

## F##

