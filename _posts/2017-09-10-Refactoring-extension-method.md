---
layout: post
title:  "Refactoring extension method"
date:  2017-09-10
categories: [C#]
---

I was assigned to do a code review on this extension method

```c#
public static bool ToBoolExtended(this object obj) 
{
  if (obj != null)
  {
      if (obj.ToString() == "0")
          return false;
      else if (obj.ToString() == "1")
          return true;

      return Boolean.Parse(obj.ToString());
  }
  else
  {
      return false;
  }    
}
```

It compilies and does the job, but is it good? Can it be improved? Let's figure that out.

## Goal

Before we get into any refactor, let's define what our code is supposed to do. Looking at the name we can figure out that it converts *something* to boolean value. That *something* can be *anything*.

Taking implementation we can tell that the `ToBoolExtended` method actualy parses an unspecified `object` type instance to boolean. For example, an instance take values of `1`, `0`, `true`, `false` or even `null`.

Let's get to the code now.

## Inverted `If ` statement

We have defined general specification what our method should do.

Let's focus now on `if else` blocks.

Nesting `if` blocks is not a good practice and can lead into so called [Pyramid of doom](https://en.wikipedia.org/wiki/Pyramid_of_doom_(programming)). We should avoid that and flatten our code as much as possible (resonably, of course).

We can replace our `if else` blocks with only `if` statments and inverting condition. This is called [inverting IFs](https://stackoverflow.com/questions/1891259/why-does-resharper-invert-ifs-for-c-sharp-code-does-it-give-better-performance) and if you have Resharper installed, you probably are being notified about this all the time.

Our method can flatten to something like

```c#
if (obj == null)
  return false;

if (obj.ToString() == "0")
  return false;

if (obj.ToString() == "1")
  return true;

return Boolean.Parse(obj.ToString());
```

This way we eliminated `else` blocks by continously eliminating edge cases.

## `? :` syntax

We can make things even simpler (and more elegant) by using `? :` [syntatic sugar](https://en.wikipedia.org/wiki/Syntactic_sugar). `? :` eventualy evaluate to simple `if else` block, but we shouldn't care about it. Don't worry, there is no performance drop here.

Let's try replacing our `ifs` 

```c#
return obj == null ? false
     : obj.ToString() == "0" ? false
     : obj.ToString() == "1" ? true
     : bool.Parse(obj.ToString());
```

This actually looks a bit more functional. C# has functional aspects and this is one of them.

A cool thing in this approach is that any unit test will always cover 100% code, because of the way the code coverage analyzers work.

A downside is debugging will be very limited.


## `ToString()`

Another thing, which all perhaps noticed at first glance is multiple `ToString()` invocation. In whole method there are three of these. This really uncool since we all know how much trouble strings can make. It is very likely that this extension method  will used within some loop so multiplication comes here as well.

Let's limit calling `ToString()` by introducing a new variable in the method.

```c#
var s = obj.ToString();
return s == "0" ? false
     : s == "1" ? true
     : bool.Parse(s);
```

We gain significant performance improvement by adding only an extra variable.

## Refactored version

So after refactor we have something like this

```c#
public static bool ToBoolExtended(this object obj) {
  if(obj == null)
    return false;
  
  var s = obj.ToString();
  return s == "0" ? false
       : s == "1" ? true
       : bool.Parse(s);
}
```

This looks way better the initial version. Less code, less branches and well formatted. Neat.

## Testing

This code is quite easy to test. Actualy, we can do it by writing one-liner.

I used C# REPL (in VS it's called Interactive C#) to quickly check my implementation.

I copy pasted body of the method to `Func<object, bool>`.

```c#
Func<object, bool> isCool = obj => {
  if(obj == null)
  	return false;
  
  var s = obj.ToString();
  return s == "0" ? false
       : s == "1" ? true
       : bool.Parse(s);
};
```

Then I created a simple satement that should return `true`.

```C#
var cool = isCool(1) & !isCool(0) & isCool("true") & !isCool("false") & !isCool(null);
```

After getting this into REPL memory and calling `cool`, we get result of above statement.

![]({{site.url}}/static/img/posts/Refactoring-extension-method/repl.gif)

Another way is to write a unit test. In xUnit2, we can do it like this, by providing all inputs that we're interested in.

```c#
[Theory]
[InlineData(1, true)]
[InlineData(0, null)]
[InlineData("true", true)]
[InlineData("false", false)]
[InlineData(null, false)]
public void should_parse_to_logical_value(object sut, bool result) => 
  sut.ToBoolExtended().ShouldBe(result);
```

## End notes

Generally parsers of any kind can be quite complicated. To make things simpler we should narrow down supported types as much as we can by declaring only supported types. It would be nice to split this method into two methods accepting `int?` and `string`. To handle case when we are not sure of which type the argument is, we should create the third method which would call those two methods internally. This is what I would do.

And one more thing. This method will be available to be called on `anything` in our system since every type inherits after `Object` class. **This is actualy the worst thing about this extension.** Adding extensions to `object` type should limited to minimum.