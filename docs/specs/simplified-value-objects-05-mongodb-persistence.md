# Simplified value objects 05: generic persistence adapters (MongoDB)

**Status:** Draft implementation plan  
**Scope:** Replace per-type scalar BSON serializers with a generic MongoDB adapter for Structur scalar value objects while preserving BSON shapes, query translation and permissive legacy loading. Depends on scalar contracts in [plan 01](simplified-value-objects-01-contracts.md), with complex/missing-field policies coordinated with [plan 03](simplified-value-objects-03-complex-value-objects.md) and [plan 04](simplified-value-objects-04-mapping.md).

## Goal and context

Consumers currently need many `IBsonSerializer<T>` implementations and registrations. Replace scalar-specific boilerplate with one generic serializer/provider without changing stored BSON representation or making persistence stricter. MongoDB is the actual repository scope; EF Core is not present and must not be added as part of this work.

The read path is trusted, permissive rehydration, not validated command construction. Historical aggregates may violate current creation rules and must remain loadable when their stored representation is viable.

This is the persistence portion of the original request to reduce value-object integration code for Structur and consumers while retaining domain meaning; see the [user story](../user-stories/value-objects.md).

## Scope and non-goals

In scope: scalar serializer/provider, registration lifecycle, type-specific primitive codecs, query metadata, legacy date/number shapes, missing-field policy, diagnostics and regression fixtures.

Out of scope: EF Core or new database dependencies, `EntityId`/`EntityTag` replacement, broad rewrites of aggregate repositories or records, strict validation on reads, read-repair writes, and replacing the current BSON round-trip `MongoDbRecordMapper` before parity is proven.

## Existing implementation

- [`StructurBsonSerialisation`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/StructurBsonSerialisation.cs) registers `EntityId<>`, discovers concrete serializers in Infrastructure and consumer assemblies, and currently catches all serializer registration exceptions.
- [`EmailAddressBsonSerialiser`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/EmailAddressBsonSerialiser.cs) stores a string and reads through `EmailAddress.Convert`.
- [`EffectiveDateBsonSerialiser`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/EffectiveDateBsonSerialiser.cs) stores BSON datetime milliseconds; [`UnitsBsonSerialiser`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/UnitsBsonSerialiser.cs) stores an Int32.
- Many per-type serializers are in [`Serialisation`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/). `EntityId` and `EntityTag` have dedicated serializers and are not scalar value-object migration targets.
- [`MongoDbRecordMapper`](../../src/Synonms.Structur.Infrastructure.MongoDb/Aggregates/MongoDbRecordMapper.cs) performs BSON round-trip mapping. [`EmployeeRecord`](../../src/sample/Synonms.Structur.Sample.Api/Features/Employees/Persistence/EmployeeRecord.cs) uses nullable domain VOs, nested objects and initialized collections.
- The record model rationale is in [`persistence-records.md`](persistence-records.md).

## Proposed design and mandatory load policy

Implement a generic `IBsonSerializationProvider` or equivalent provider for supported scalar contract types. Supply shared primitive codecs selected by underlying type so a normal consumer value object needs no codec or serializer of its own. Allow explicit per-type overrides only for exceptional historical representations. Encoding/decoding must preserve existing string, integer width, decimal and BSON datetime/date-only representations and documented legacy formats. Register the provider once before first BSON type resolution. Retain precedence for explicit/custom serializers; future consumers should not need one serializer per scalar type.

Support query metadata (`IBsonDocumentSerializer`/`IBsonArraySerializer` as applicable) so LINQ/filter translation can see the wrapped primitive member and array element shape. Verify existing scalar LINQ queries and date queries; a correct stored value alone is insufficient if filters stop translating.

**Read policy:**

1. Persistence adapters must enter through the type's `Rehydrate`/existing `Convert`, preserving exact current `Convert` behavior for initial rollout. Do not call `CreateMandatory`/`CreateOptional` directly and reject legacy records on validation failure. Existing `Convert` implementations may internally consult validation to choose a permissive fallback; retaining that internal behavior is compatible with this policy.
2. Separate four cases: (a) missing field; (b) explicit BSON null; (c) invalid under current business rules but representable in the existing BSON type; (d) malformed or unsupported BSON shape that cannot be represented. Cases (a)-(c) use the documented per-type/property policy; case (d) raises a diagnostic deserialization error and is not broadly caught. Where a field has known historical BSON shape variants (including array/object representation changes), explicitly support and test each documented legacy shape; do not assume a new current shape makes old records corrupt.
3. Preserve representable legacy values where practical. Where current `Convert` substitutes a domain-owned viable fallback (for example `EmailAddress.Convert` returns an empty value on invalid data), keep that behavior initially. Record each current behavior before migration; any preserve-vs-substitute change requires explicit review and migration compatibility tests.
4. Missing fields never invoke scalar serializers. Optional missing/null values normally remain absent. Required missing values need a deterministic, explicit property/type rehydration policy, implemented in record initializers or aggregate construction/mapping. Never generically fabricate identities, tenant/security/audit data. A domain-only sentinel may fail current create validation.
5. Emit a diagnostic when a default/substitution is used through repository logging/diagnostic conventions, with type/property/reason only—never raw sensitive values. No read may mark audit, change persisted `EntityTag`, emit events or automatically rewrite the database.

**Write policy:** Serialize the current underlying primitive at existing precision/shape. Do not validate round-trips during writes; an unchanged legacy value must not become unreadable or unwriteable just because current rules changed. Do not round decimal values for storage. Entity IDs/tags keep their dedicated serialization.

Use explicit/idempotent registration state to avoid duplicate registration. Do not use a blanket catch as duplicate handling: handle known already-registered conditions narrowly, preserve custom serializer precedence, and let unexpected setup failures surface. Because Mongo serializer registries can be process-wide and immutable after type resolution, document startup ordering and test isolated registration behavior.

For record/domain aggregate loading, reuse nullable record defaults and add explicit construction mapping where absent fields require policy. Preserve BSON round-trip mapping initially; replacing it needs separate parity evidence. Restore stored entity tags exactly; do not let constructor defaults regenerate them on reads.

## Implementation sequence

1. Inventory every existing scalar serializer and record its BSON token/width/null behavior, current `Convert` behavior and query metadata. Include legacy date formats by examining fixtures/real test documents before designing conversion.
2. Design shared primitive codec/metadata selection and the generic serializer. Provide per-type overrides only where historical representations differ from the shared underlying-type codec.
3. Add generic provider/query metadata. Register once at the existing Mongo startup registration entry point before any affected type is resolved; retain `EntityId`/`EntityTag` and explicit custom serializer precedence.
4. Migrate a representative string, integer and date-only scalar. Add BSON-shape, null, legacy invalid-representable, malformed-shape, query serialization and round-trip tests.
5. Migrate remaining serializers incrementally; remove a per-type serializer only when its compatibility and query fixtures pass.
6. Add record-level missing/null policy and end-to-end aggregate read/update fixtures. Do not change `MongoDbRecordMapper` unless a separately measured parity replacement is approved.
7. Document consumer registration and the no-per-type-registration path.

## Acceptance criteria and targeted regression verification

- One generic adapter handles supported scalar value objects; BSON primitive type, width, precision, null handling and date representation match existing fixtures.
- Scalar LINQ/filter translation continues to target stored primitive fields and arrays; targeted date filters/legacy formats remain correct.
- Existing valid and invalid-representable records load under their per-type current behavior, including `EmailAddress`'s current fallback. Business validation is not run on read.
- End-to-end fixtures cover an old record missing a newly required value; an old value now outside today's rules; successful permissive read; invalid update rejected with domain faults; corrective update succeeds; read causes no write/audit/event/tag change.
- Missing optional/null fields remain absent as specified; missing required values receive explicit deterministic per-property behavior; malformed BSON shapes fail diagnostically rather than being swallowed.
- Writes of unchanged rehydrated values preserve BSON shape and precision. Registration is idempotent and does not override explicit serializers or affect `EntityId`/`EntityTag`.
- Add tests to the actual available test projects (Domain, Core, API Server or sample integration as appropriate); there is no dedicated MongoDB test project currently. Use sample integration/persistence tests or an appropriate existing unit project, and build Infrastructure.MongoDb.

## Compatibility, rollout and pitfalls

Preserve the current serializer and record path until each type has compatibility coverage. No read-repair or automatic database rewrite. The current sample aggregate [`Employee`](../../src/sample/Synonms.Structur.Sample.Api/Features/Employees/Employee.cs) uses scoped/versioned updates and `UpdateMandatoryValue`; its `AggregateRoot<T>` base currently skips a mandatory update when the old property is null. If a newly required value is rehydrated as null or sentinel, test that the intended corrective update actually replaces it. Fix only this directly coupled gap, without adding whole-aggregate validation or bypassing the existing operation/version scope.

Do not confuse a serializer's handling of explicit null with missing-document-field behavior. Do not broad-catch corrupt BSON and return a successful-looking default. No EF Core dependency is in scope.
