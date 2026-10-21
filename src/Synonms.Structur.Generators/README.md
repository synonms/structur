# Structur storage generator

This analyzer-only package generates bounded plain persistence records for types opted into
`Synonms.Structur.Domain.Persistence.GenerateStorageAttribute`. Consumers must reference the
Structur Domain and Infrastructure.MongoDb runtime packages separately; the generator package
does not add a runtime Roslyn dependency.

Opt in a concrete `partial` aggregate and classify every declared state property:

```csharp
[GenerateStorage("employee", 1)]
public sealed partial class Employee : AggregateRoot<Employee>
{
    [StoredState("forename")]
    public Moniker Forename { get; private set; }

    [NotStored]
    public string FullName => Forename.Value;
}
```

The generated `EmployeeStorageDescriptor.CreateMapping()` returns the typed mapping used by
`MongoDatabaseConfiguration`. `DomainToRecordPaths` and `DomainToRecordCodecs` expose stable
metadata. Unsupported graphs produce compile-time `STOR001`–`STOR007` diagnostics; non-generated
or exceptional mappings should use the handwritten `IAggregateRecordMapper<TAggregate,TRecord>`
contract.
