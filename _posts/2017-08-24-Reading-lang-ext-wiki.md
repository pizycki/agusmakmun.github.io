---
layout: post
title:  "Introduction to FP in C#"
date:  2017-08-24
categories: [FP, C#]
---

**TL;DR:** Notes taken during read of [louthy](https://github.com/louthy)/[**language-ext**](https://github.com/louthy/language-ext) [wiki](https://github.com/louthy/language-ext/wiki/)

Based on great work of [Paul Louth](https://github.com/louthy), author of [Language-Ext library for functional programming](https://github.com/louthy/language-ext). I highly recommend reading full version [lang-ext wiki](https://github.com/louthy/language-ext/wiki/). It's like the ABC-book for C# functional programmer wannabe. 

If you like Pauls work, make sure you [support him](https://github.com/louthy/language-ext/issues/223).

The notes are generally for me which explains the poor format. Some of the text below is raw *copy/paste*.

The wiki has been adapted from the excellent [fsharpforfunandprofit](https://fsharpforfunandprofit.com/series/thinking-functionally.html) site and is provided under the terms of [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/).

---

* [Pure function](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Mathematical-functions#the-power-of-pure-functions)

  * no side effect
  * deterministic
  * 1 param, 1 result ONLY

* binding - process of using a name to represent a value
  * in F#: `let` and `const`/`readonly` in C#

* Values
  *  immutable

* [Simple value](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Function-values#simple-values) 
  * does not need to be evaluated after being bound
  * `Func<Unit, int> C = _ => 5;`

* [Function value](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Function-values#function-values)
  * `Func<domain, range>`
  * functions are values that can be passed around as inputs to other 

* [Higher-order function (HoF)](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-How-types-work-with-functions#function-types-as-parameters)

  * function that takes other functions as parameters, or returns a function
  * LINQ examples: `Select`, `Where`, `Aggregate`

*  [Unit](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-How-types-work-with-functions#the-unit-type)

  * in type-theory `void` represents a type with no possible values (it has no domain)
  * use `ignore()` for `any -> unit`

* [Curring](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Currying)

  * breaking multiple parameter functions into smaller one parameter functions
  * leads to a very powerful technique called partial function application
  * `any -> any -> any`

  ```c#
  // step by step version
      var x = 6;
      var y = 99;
      var intermediateFn = printTwoParameters(x); // return fn with 
                                                  // x "baked in"

      var result  = intermediateFn(y); 

      // inline version of above
      var result  = printTwoParameters(x)(y);
  ```

* Partial function application

  * The idea of partial application is that if you fix the first N parameters of the function, you get a function of the remaining parameters. 

  * ```c#
    // create a logging function that writes to the console
    static Unit ConsoleLogger<A>(string argName, A argValue){
        Console.WriteLine($"{argName}={argValue}");
        return unit;
    }

    // create an adder with the console logger partially applied
    static Func<A, A, A> AddWithConsoleLogger<MonoidA, A>() 
        where MonoidA : struct, Monoid<A> =>
            par(AdderWithPluggableLogger<MonoidA, A>, ConsoleLogger);

    // create an adder that works with ints
    var addIntsWithConsoleLogger = AddWithConsoleLogger<TInt, int>();

    // Test
    addIntsWithConsoleLogger(1, 2);
    addIntsWithConsoleLogger(42, 99);

    // create a logging function that creates popup windows
    static Unit PopupLogger<A>(string argName, A argValue){
        var message = $"{argName}={argValue}";
        System.Windows.Forms.MessageBox.Show(text: message, caption: "Logger");
        return unit;
    }

    // create an adder with the popup logger partially applied
    static Func<A, A, A> AddWithPopupLogger<MonoidA, A>()
        where MonoidA : struct, Monoid<A> =>
            par(AdderWithPluggableLogger<MonoidA, A>, PopupLogger);

    // create an adder that works with strings
    var addStringsWithPopupLogger = AddWithPopupLogger<TString, string>();

    // Test
    addStringsWithPopupLogger("Hello, ", "World");
    addStringsWithPopupLogger("Really ", "Generic");
    ```

* [Function composition](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Function-composition)
  * Because of a limitation in how C# treats 'method groups', you will have to provide the generic arguments if you're working with static methods. One way around that is to declare your static methods as `readonly static` fields.

  * ```c#
    var h = compose(f, g);
    ```

* [Combinators](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Combinators)

  * The word "combinator" is used to describe functions whose result depends only on their parameters. That means there is no dependency on the outside world, and in particular no other functions or global value can be accessed at all.
  * Example: `compose` function
  * [Combinator birds](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Combinators#combinator-birds)
  * safest type of function - they have no dependency on the outside world
  * [LanguageExt.Parsec](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Combinators#combinator-libraries) use

* [Function Signatures](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Function-Signatures)

  * [Some boilerplate for defining our types](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Function-Signatures#constrained-types)

  * Don't overuse primitive types, use types instead

  * Usage of `Try`, `Option` and linq to avoid nested `Match`

    * `Try` is for handling exceptions (like *I know it throws exception and can deal with it*)

    * `Option` is like `int.Parse(..., out int ...)` in C#, I don't guarantee you get what you want, but at least you don't get `null`.

    * `Try` and `Option` are very similiar, but in `Try` you get exception on output (which you can handle with), in `Option` you get nothing.

    * Both are subset of `Either` (which once you understand, you get on with anything else)

    * With both you can write LINQ selects

      ```C#
      Option/Try<int> result = from a in ParseInt("10")
                               from b in ParseInt("20")
                               from c in ParseInt("30")
                               select a + b + c;
      ```

* [Application Architecture](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-Application-Architecture)

  * Organize functions in **Modules**
    * Module is `static` `class` with `static` methods, with no `static` state (shared variables etc.)
    * In OO this is called [anaemic domain model](https://martinfowler.com/bliki/AnemicDomainModel.html)
    * Gang of Four is responsible for book [Design Patterns: Elements of Reusable Object-Oriented Software](https://en.wikipedia.org/wiki/Design_Patterns)
  * Creating a project specially for all record types is a good things and can be called **Schema**
    * Optionally, you can store record helpers that does nothing more, but manipulates data without changing them (`GetFullName` on `Person` record)
    * You can use the `Record<A>` feature of language-ext to help you build immutable record types that have structural equality by default
  * You can choose one of the approaches to build your system: `bottom->up` or `top->down`
    * In `bottom->up` you start from elemental blocks
    * In`top->down` you start from what you have what you want to have
  * IO in FP is awkward 
    * Limit any IO to minimum (one SQL select or one save on Unit of Work (old habbits from OO, but I wonder you can deal with it in FP world...))
  * Free monad is a good way to deal with IO
    * Unfortunately its hard to build generic free monad in C#
    * See [sample](https://github.com/louthy/language-ext/tree/master/Samples/BankingAppSample)
    * Its a little like describing an interface
  * Actors
    * `State -> Message -> State`
    * single threaded
  * Inheritance isn't always bad. It's just nearly always bad.

* [LINQ](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-What-is-LINQ-really%3F) - [Why should I care about monads?](https://github.com/louthy/language-ext/wiki/Thinking-Functionally:-What-is-LINQ-really%3F#why-should-i-care-about-monads)

  * *"a monad is a monoid in the category of endofunctors, what's the problem?"*

  * **What are monads for?** Succinctly they're design patterns that allow you to stop writing the same error prone boilerplate over and over again.

  * `Option` is monad that can be in two states, `Some` and `None`

  * [Cyclomatic Complexity](https://en.wikipedia.org/wiki/Cyclomatic_complexity) is reduced with `Option`

  * monad(s) in LINQ 

    ```c#
    from a in ma
    from b in mb
    ```

    *"As before this is saying "Get the value `a` out of monad `ma`, and then if we have a value get the value `b` out of monad `mb`". So for `IEnumerable` if `ma` is an empty collection then the second `from` won't run. For `Option` if `ma` is a `None` then the second `from` won't run."*

  * C# doesn't support '[higher kinded types](https://en.wikipedia.org/wiki/Kind_(type_theory))'

  * The methods `Select` and `SelectMany` are kind of hacks in C#. They're special case methods that make a type monadic.


---

Books:

* "Functional Programming in C#" by Enrico Buonanno

* "To Mock a Mockingbird" by Raymond Smullyan


Casts:

* [Are We There Yet?](https://www.infoq.com/presentations/Are-We-There-Yet-Rich-Hickey)
* [Railway oriented programming: Error handling in functional languages](https://vimeo.com/113707214)