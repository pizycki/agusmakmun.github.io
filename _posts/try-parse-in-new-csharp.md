Let's quickly refactor this piece of code to more modern form.

```csharp
private int? MapAbsenceReason(string reason)
{
    int? result = null;
    AbsenceReasonEnum absenceReason;

    if (Enum.TryParse(reason, true, out absenceReason))
        result = (int)absenceReason;

    return result;
}
```
This is common `TryParse` method from .NET standard library.

With C#6 and 7 features, some things can simplified.

Here is how we can rewrite this method.

```csharp
private static int? MapAbsenceReason(string reason) => 
    Enum.TryParse(reason, true, out AbsenceReasonEnum absenceReason)
      ? (int?)absenceReason
      : null;
```

Let's point some things
* We made method static. This way we encourage to do the things stateless (or _state-careful_ :) )
* The method in modern form is much shorter. With [expression-bodied method (C#6)](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/statements-expressions-operators/expression-bodied-members), [?: operator](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/conditional-operator) and [out parameter (C#7)](https://social.technet.microsoft.com/wiki/contents/articles/37675.c-7-0-out-parameter.aspx) we changed the code from imperative to expression-like.
  With less code comes less bugs, so there is profit too ;)
* We removed mutation. Yes, there is still assigning hidden in `Enum.TryParse` method, but it occurs only once. No further mutation, no more worries. The code looks much simpler this way.

Unfortunetly, the new form is less _traditional_ and those who prefer C-style languages to more functional might don't like the new form. They'd say "it's harder to understand".

I see it this way. The second form is just... less common. C#7 has barely been released and its syntax has not been yet adopted by most of the developers.

Even though I use modern C# syntax in my every day work. I just don't see the reason why I should not. The code evolves by its nature, day after day. And if we keep write the same way all the time, we might end up with code that none want to work with.
Developers like new, shiny things (frameworks, languages etc). Why forbidd using them if they don't impact on performence?
