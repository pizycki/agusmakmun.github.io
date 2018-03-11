---
layout: post
title:  "Notes from reading Becoming Functional"
date:   2000-01-01
categories: [.NET, FP]
---


## Closure
Closures reference variables outside their scope.

```csharp
public string BuildFoobar() {
  var a = "foo";
  var b = "bar;

  return concat();

  string concat() = a + b; // Local function can be a Closure.
}
```
