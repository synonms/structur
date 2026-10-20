# Simplified value objects 03: complex value objects

**Status:** Implemented — domain-owned DTO contract, conventional API output mapping and MongoDB DTO delegation

**Scope:** Object-shaped value objects, independent of the scalar hierarchy. `IComplexValueObject<TSelf,TData>` is the construction/mapping contract; `ComplexValueObject<TSelf>` is optional equality support.

## Domain contract

[`IComplexValueObject<TSelf,TData>`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/IComplexValueObject.cs) lives in Domain:

```csharp
public interface IComplexValueObject<TSelf, TData>
    where TSelf : class, IComplexValueObject<TSelf, TData>
    where TData : class
{
    TData ToData();
    static abstract OneOf<TSelf, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, TData data);
    static abstract OneOf<Maybe<TSelf>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, TData? data);
    static abstract TSelf Rehydrate(TData data);
}
```

`Address`, `Period`, `EmailContact`, `TelephoneContact` and `UkBankDetails` implement this contract with an adjacent `*Data` class. These DTOs have no API, JSON or MongoDB dependency. Members are raw scalar values; nested complex members in consumer DTOs should use their own data contracts rather than domain instances. Nullable string, integer and boolean members represent incomplete input, not an assertion that a successfully constructed value may lack required state.

`UserAction` remains outside this contract: it is audit state, with `UserActionDto`, `UserActionRecord` and its existing persistence lifecycle. This change does not invent audit identities or change audit timestamps, tags or events.

### Validated construction

- Existing multiparameter factories forward to DTO overloads, preserving source compatibility.
- `CreateMandatory` and `Rehydrate` trust their non-nullable DTO contracts and do not guard against null arguments. Callers that bypass nullable warnings and supply null are misusing the contract and may encounter a `NullReferenceException`; this is not a domain validation failure.
- Missing required DTO members produce existing validator faults, using paths such as `Address.Line1`, `Period.Units` and `Email.IsPrimary`. Missing strings are passed to existing scalar validators as empty input rather than causing null dereferences.
- Optional null DTOs return `None`. Existing partial-input behavior is preserved: an address missing any of type/line1/postcode, a period missing units/interval, a contact missing type/address-or-number/primary flag, or bank details missing any required component returns `None`, even if other supplied components are invalid.
- Complete optional input goes through mandatory validation. A boolean `false` and integer `0` are populated input, not absence.

### Projection, reconstruction and equality

`ToData` projects current state directly; it does not validate or reconstruct. Required scalars are projected through `Value`, optional scalars through nullable `Value`. Collection-bearing implementations must own their input and return fresh collections or immutable snapshots, never expose mutable owned collections.

`Rehydrate` constructs directly and calls each nested scalar's **existing** `Rehydrate`. The built-in complex methods never call complex `CreateMandatory`. Scalar policies have intentionally not been rewritten: for example, invalid persisted email input still becomes the existing empty email fallback, and negative units still become the existing zero fallback. A projection of that state must remain possible even when today's mandatory factory rejects it.

For the five built-ins, every required raw DTO member must be non-null during reconstruction. [`ComplexValueObjectData.Required`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/ComplexValueObjectData.cs) rejects missing/null required members rather than constructing a broken graph or fabricating a sentinel. Optional missing/null members remain null. Empty or otherwise representable non-null input enters the nested scalar's established policy.

Equality is exact runtime type and member-wise value equality, with matching hash codes. Built-ins compare their scalar members' raw values, not DTO identity. [`ComplexValueObjectEquality`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/ComplexValueObjectEquality.cs) supports ordered sequence equality and recursive sequence hashing; record/list reference equality is not a substitute. Collection order matters. The integration-only address-book fixture demonstrates ownership and ordered equality, including derived-type inequality, without adding an unrelated framework domain class.

The five built-ins and address-book fixture inherit [`ComplexValueObject<TSelf>`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/ComplexValueObject.cs), overriding only protected `object?[] EqualityMembers`. The base supplies typed `IEquatable<TSelf>`, object equality and hashing with null/reference/exact-runtime-type checks. It has no DTO parameter or construction responsibilities. Serializer and mapper discovery still uses the interface, allowing types with another base to supply their own equality.

## API output mapping

[`ComplexValueObjectResourceMapping`](../../src/Synonms.Structur.Api.Server/Mapping/ComplexValueObjectResourceMapping.cs) supplies:

- separate complex-contract detection via Domain type extensions, not `IsValueObject`;
- a cached typed bridge to `ToData`;
- cached compiled member read/write bindings by data/resource type pair;
- conventional public property-name matching, nested data/resource mapping, null preservation, arrays and list-compatible enumerable destinations;
- raw string-to-API-enum conversion, e.g. `AddressData.Type` to `AddressResource.Type`, retaining case-sensitive enum parsing;
- preservation of unmatched resource defaults/metadata.

`DefaultResourceMapper`, `DefaultChildResourceMapper` and `MappingHelper` route complex output through this path. Existing aggregate/resource links, IDs and routing remain owned by their normal mappers. Complex collections preserve ordering and null elements. Built-ins need no handwritten mapping adapters.

For exceptional/noncanonical shapes, register a typed output adapter at startup:

```csharp
ComplexValueObjectResourceMapping.Register<MyValue, MyResource>(value => /* explicit output projection */);
```

Adapters take precedence, including for nested data/resource pairs. Arbitrary concrete collection destinations beyond arrays and types assignable from `List<T>` require an adapter.

Nested projections contain data DTOs, not their original domain objects: register an `AddressData`-to-resource adapter for an address inside another projected DTO. An `Address`-to-resource adapter only applies when the source is an `Address`. Adapters are consulted even after conventional plans are cached.

Collection element types are resolved from their actual `IEnumerable<T>` contracts, including inherited interfaces. Ambiguous/non-generic enumerables and non-vector arrays require explicit adapters rather than falling through to object-property mapping.

**API input is unchanged:** resources deserialize into resource DTOs through existing JSON converters, then aggregate/value-object factories validate them and aggregate domain faults. There is no default complex domain JSON converter, no resource-to-domain permissive mapping and no conversion of domain faults into `JsonException`.

## MongoDB persistence and compatibility

[`ComplexValueObjectBsonSerializationProvider`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/ComplexValueObjectBsonSerializationProvider.cs) selects the contract-based [`ComplexValueObjectBsonSerialiser`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/ComplexValueObjectBsonSerialiser.cs). It delegates BSON storage to the registered `TData` serializer and reconstructs with `TSelf.Rehydrate`.

Call `StructurBsonSerialisation.TryRegisterSerialisersFrom(...)` before MongoDB first resolves affected types. Explicit serializers from the framework/consumer assemblies retain precedence; providers are registered once. Custom serializers remain the escape hatch for noncanonical historical shapes.

### Characterized legacy shape

Before replacing the old automatic class-map path, an integration characterization confirmed that `Address` stores a PascalCase object with raw scalar fields and explicit optional BSON nulls; valid documents round-trip. The old reader left required members null when missing or explicitly null. The new required-member rejection is an intentional diagnostic tightening for these invalid graphs, not a scalar fallback change.

Integration tests compare the new DTO-backed output against the old `BsonClassMapSerializer<T>` output for **all five** migrated built-ins. Names, field ordering, primitive shapes and optional nulls agree. No global naming/discriminator convention changes are introduced. Consumers using bespoke class maps/conventions that differ between domain and data types must retain a custom serializer or configure matching DTO maps.

### Read/write policy

| Persisted state | Policy |
|---|---|
| Whole optional complex value missing or BSON null | Remains null under the record's existing defaults / serializer null handling |
| Required DTO member missing or explicit null | Reconstruction fails with a diagnostic `BsonSerializationException` |
| Optional member missing/null | Remains absent |
| Representable member outside current validation | Nested scalar `Rehydrate` keeps its existing fallback/preservation policy |
| Alternate primitive representation supported by Mongo's raw serializer | Remains supported, e.g. representable string boolean input |
| Malformed/unrepresentable member shape | Diagnostic failure; no catch-and-default graph |

Diagnostics identify the complex contract and missing/null-or-malformed reason, excluding raw stored values and unsafe inner exception messages. Writes use `ToData` without revalidation. Reads do not repair records or modify audit state.

`IBsonDocumentSerializer` metadata delegates field names to the DTO document serializer but supplies the actual domain member type/serializer, allowing nested scalar filters and complex collection element queries. The existing BSON round-trip [`MongoDbRecordMapper`](../../src/Synonms.Structur.Infrastructure.MongoDb/Aggregates/MongoDbRecordMapper.cs) is unchanged.

Custom domain scalar/member serializers must match the DTO's raw storage representation for query operands to agree with stored data. Default-shape parity does not guarantee parity for bespoke serializers; retain a custom complex serializer when those representations cannot be aligned.

## Verification and deferred work

[`Synonms.Structur.Tests.Integration`](../../tests/Synonms.Structur.Tests.Integration) contains framework-only integration fixtures for generic factories, nested validation paths, optional semantics, reconstruction/projection, exact-type/ordered equality, collection ownership, resource JSON shape/enums/nulls, legacy BSON parity, record round trips, missing/null/malformed data, query member metadata and explicit adapter/serializer precedence. No live MongoDB server is needed; these tests exercise the MongoDB driver's serializers, filters and record mapper.

Relevant commands:

```powershell
dotnet test --project tests/Synonms.Structur.Tests.Integration/Synonms.Structur.Tests.Integration.csproj --no-restore
dotnet test --project tests/Synonms.Structur.Api.Server.Tests.Unit/Synonms.Structur.Api.Server.Tests.Unit.csproj --no-restore
dotnet test --project tests/Synonms.Structur.Domain.Tests.Unit/Synonms.Structur.Domain.Tests.Unit.csproj --no-restore --filter-class '*ValueObjectBsonSerialiserTests'
```

Deferred: source generation, a total mapping-plan rewrite, replacement of BSON record mapping, migration of consumer classes, arbitrary collection construction, polymorphic complex DTO/discriminator contracts, and live-database query execution. Scalar fallback policies and contradictory raw-preserving assertions in `ValueObjectContractTests` are outside this change. The preexisting scalar `ValueObject.Equals(object)` recursive dispatch is also unchanged; complex member equality does not depend on that path.
