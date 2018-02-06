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

```csharp
private static int? MapAbsenceReason(string reason) => 
    Enum.TryParse(reason, true, out AbsenceReasonEnum absenceReason)
      ? (int?)absenceReason
      : null;
```

* made static
* shorter 10 v 4
  * less code -> fewer lines for bugs
* no mutation
