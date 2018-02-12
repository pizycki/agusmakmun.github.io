---
layout: post
title:  "How to assert your collection is ordered"
date:   2018-02-12
categories: [tests, .NET]
---

In [PagiNET](https://github.com/pizycki/PagiNET), the library which I'm currently working on, there is a lot of code that's supposed to order things in ascending and descending way.

There are many assertion libraries, but ~~none of them~~ none in my knowledge can perform a simple test against a collection - check if it's ordered or not. Literally to check that every element in the sequence is in the desired position.

Obviously, such helper was unavoided. I wrote an implementation using ~~fold~~ `.Aggregate` function in LINQ standard library.

> If you don't know yet the most _sexy_ and the simplest `Aggregate` example, check it out [here](http://izzydev.net/devoweek/2017/12/03/devoweek.html).

There are two functions:

* First, `IsFirstItemTheBiggest`, checks if the first element in the sequence is the greatest one.
* Second, `IsOrderedAscending`, applies the first function on `n!` sets, taking as first one the original sequence.

They both accept comparers to compare items in sequence. Unfortunately, we have to ~~implement~~ copy/paste comparers for each type we'd like to handle in our tests. Of course we're talking about algebraic types right now, but nothing prevent us do the same for **any type we want**.

With functional approach there is less code. Just look at the equality comparer (`LongsEquals`). It's just one line of code! Imagine how much code you'd have to write in OOP way (implement class, extract interface and register it with container). That's what I really like here.

Everyone should get that already, but let's make it clear: **it is not the fastest way to check weather the sequence is ordered or not, but me personally, I find it very simple to understand**. Mainly this is because of reduced mutations to minimum. Oh, and by the way, this solution **scales out**, which means, it can be processed in parallel.

Below I present both of the functions with their alternatives (`IsLastItemTheBiggest` and `IsOrderedDescending`). Each one of them has their own set of tests included too.

Feel free to use them in your projects.

```csharp
public static class ItemIsTheBiggest {
  public static bool IsFirstItemTheBiggest<TItem>(
    IEnumerable<TItem> items,
    Func<TItem, TItem, int> compare,
    Func<TItem, TItem, bool> equal) {
    if (items == null) throw new ArgumentNullException(nameof(items));
    if (!items.Any()) throw new ArgumentException("Collection is empty.");
    var max = items.Aggregate((a, b) => compare(a, b) != -1 ? a : b);
    return equal(items.First(), max);
  }

  public static bool IsLastItemTheBiggest<TItem>(
    IEnumerable<TItem> items,
    Func<TItem, TItem, int> compare,
    Func<TItem, TItem, bool> equal) {
    if (items == null) throw new ArgumentNullException(nameof(items));
    if (!items.Any()) throw new ArgumentException("Collection is empty.");
    var max = items.Aggregate((a, b) => compare(a, b) != 1 ? b : a);
    return equal(items.Last(), max);
  }
}

public class ItemIsTheBiggestTests {
  [Fact] 
  public void checking_the_first_item_is_the_biggest_works() =>
    IsFirstItemTheBiggest(
    items: new long[] { 5, 4, 3, 3, 2, 1 },
    compare: Comparers.LongsComparer,
    equal: Comparers.LongsEquals).ShouldBeTrue();

  [Fact]
  public void checking_the_last_item_is_the_biggest_works() =>
    IsLastItemTheBiggest(
    items: new long[] { 1, 2, 3, 3, 4, 5 },
    compare: Comparers.LongsComparer,
    equal: Comparers.LongsEquals).ShouldBeTrue();

  [Fact]
  public void checking_the_first_item_is_the_biggest_but_is_not_works() =>
    IsFirstItemTheBiggest(
    items: new long[] { 2, 6, 3, 2, 6, 4 },
    compare: Comparers.LongsComparer,
    equal: Comparers.LongsEquals).ShouldBeFalse();

  [Fact]
  public void checking_the_last_item_is_the_biggest_but_is_not_works() =>
    IsLastItemTheBiggest(
    items: new long[] { 2, 6, 3, 2, 6, 4 },
    compare: Comparers.LongsComparer,
    equal: Comparers.LongsEquals).ShouldBeFalse();
}
```

```csharp
public static class OrderChecks {
  public static bool IsOrderedAscending<TItem>(
    IEnumerable<TItem> items,
    Func<TItem, TItem, int> compare,
    Func<TItem, TItem, bool> equal) {
    if (items == null) throw new ArgumentNullException(nameof(items));
    if (!items.Any()) throw new ArgumentException("Collection is empty.");

    return items
      .Select(x => items.SkipWhile(y => ReferenceEquals(x, y)))
      .Select(coll => IsLastItemTheBiggest(coll, compare, equal))
      .All(result => result == true);
  }

  public static bool IsOrderedDescending<TItem>(
    IEnumerable<TItem> items,
    Func<TItem, TItem, int> compare,
    Func<TItem, TItem, bool> equal) {
    if (items == null) throw new ArgumentNullException(nameof(items));
    if (!items.Any()) throw new ArgumentException("Collection is empty.");

    return items
      .Select(x => items.SkipWhile(y => ReferenceEquals(x, y)))
      .Select(coll => IsFirstItemTheBiggest(coll, compare, equal))
      .All(result => result == true);
  }
}

public class OrderCheckerTests {
  [Fact]
  public void checking_order_of_ordered_asc_collection_works() =>
    OrderChecks.IsOrderedAscending(
    new long[] { 1, 2, 3 },
    Comparers.LongsComparer,
    Comparers.LongsEquals).ShouldBeTrue();

  [Fact]
  public void checking_order_of_unordered_asc_collection_works() =>
    OrderChecks.IsOrderedAscending(
    new long[] { 1, 3, 2 },
    Comparers.LongsComparer,
    Comparers.LongsEquals).ShouldBeFalse();

  [Fact]
  public void checking_order_of_ordered_desc_collection_works() =>
    OrderChecks.IsOrderedDescending(
    new long[] { 3, 2, 1 },
    Comparers.LongsComparer,
    Comparers.LongsEquals).ShouldBeTrue();

  [Fact]
  public void checking_order_of_unordered_desc_collection_works() =>
    OrderChecks.IsOrderedAscending(
    new long[] { 1, 3, 2 },
    Comparers.LongsComparer,
    Comparers.LongsEquals).ShouldBeFalse();
}
```

```csharp
public static class Comparers {
  public static Func<long, long, int> LongsComparer =>
    (a, b) => a > b ? 1
            : a < b ? -1
            : 0;

  public static Func<long, long, bool> LongsEquals => (a, b) => a == b;
}

public class LongsComparersTests {
  private const int FirstGreater = 1;
  private const int FirstSmaller = -1;
  private const int BothEqual = 0;

  [Theory]
  [InlineData(1L, 2L, FirstSmaller)]
  [InlineData(0L, 0L, BothEqual)]
  [InlineData(1L, -1L, FirstGreater)]
  public void can_compare_two_values(long a, long b, int result) =>
    Comparers.LongsComparer(a, b).ShouldBe(result);
}

public class LongsEqualsTests {
  private const bool Equal = true;
  private const bool Unequal = false;

  [Theory]
  [InlineData(1, 2, Unequal)]
  [InlineData(1, 1, Equal)]
  public void can_compare_two_values(long a, long b, bool result) =>
    Comparers.LongsEquals(a, b).ShouldBe(result);
}
```
