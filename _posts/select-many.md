```
private IQueryable<Communication> GetCommunicationQuery(IQueryable<CommunicationEntity> query)
{
    return from comm in query
        group new Communication
        {
            Id = comm.Id,
            RegistrationId = comm.RegistrationId,
            TypeId = comm.CommunicationTypeId,
            TypeName = comm.CommunicationType.Name,
            CategoryId = comm.CommunicationCategoryId,
            CategoryName = comm.CommunicationCategory.Name,
            SubcategoryId = comm.CommunicationSubcategoryId,
            Subject = comm.Subject,
            ActionDate = comm.ActionDate,
            ActionBy = comm.ActionBy,
        }
        by comm.Id
        into grouping
        select grouping.FirstOrDefault();
}
```

Good:
Extract `IQueryable` (seperate _bind_ from _result_) // links! check if correct !

Bad:
Return in last line `FirstOrDefault` // Say what operations materialize in EF/LINQ

## SelectMany (a.k.a. flatMap ! ) // Check if correct

```
private IQueryable<Communication> GetCommunicationQuery(IQueryable<CommunicationEntity> query) =>
  (from comm in query
   group new Communication
   {
       Id = comm.Id,
       RegistrationId = comm.RegistrationId,
       TypeId = comm.CommunicationTypeId,
       TypeName = comm.CommunicationType.Name,
       CategoryId = comm.CommunicationCategoryId,
       CategoryName = comm.CommunicationCategory.Name,
       SubcategoryId = comm.CommunicationSubcategoryId,
       Subject = comm.Subject,
       ActionDate = comm.ActionDate,
       ActionBy = comm.ActionBy,
   } by comm.Id
     into grouping
     select grouping).SelectMany(x => x); // Flat map!
```

`Select` is not enough as we'll get `IQueryable<IEnumerable<Communication>>` type.
