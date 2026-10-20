# Framework usability 02: plain persistence, generation and server querying

**Status:** Draft for approval; proposed APIs, not implementation.  
**Parent:** [Overview](framework-usability-01-overview.md). Feeds [lifecycle snapshots](framework-usability-03-crud-events-projections.md), [durable writes](framework-usability-04-durable-delivery.md) and [rollout gates](framework-usability-06-rollout-validation.md).

## 1. Runtime foundation: raw records and typed restoration

### Proposed contracts and data shape

Retain `AggregateRootRecord<TAggregate>` as optional typed metadata with raw `Guid Id`, `TenantId`, `EntityTag` and plain `UserActionRecord` audit fields. Audit records must themselves pass recursive graph validation. Do not persist the generic type argument or any live aggregate instance. A simplified **proposed** shape:

```csharp
public sealed class EmployeeStorage : AggregateRootRecord<Employee>
{
    public string? Forename { get; set; }
    public string? NationalInsuranceNumber { get; set; }
    public DateOnly? WorkPermitValidUntil { get; set; }
    public AddressStorage? HomeAddress { get; set; }
    public List<EmailContactStorage> EmailContacts { get; set; } = [];
    public EmployeeEqualOpportunitiesStorage? EqualOpportunities { get; set; }
}

public interface IAggregateRecordMapper<TAggregate, TRecord>
{
    TRecord Capture(TAggregate aggregate);
    TAggregate Restore(TRecord record);
}
```

This is illustrative, not a complete Employee schema. Actual field inventory must include every persisted root/member/audit field and historical storage name. `Capture` owns/copies mutable data; `Restore` copies collection state rather than sharing record lists. Records contain no `Moniker`, `EntityId`, `EntityTag`, domain enum wrapper, aggregate/member, API resource/envelope, delegate or service. Store an enum as its explicitly configured raw integer/string representation, not an accidental new representation.

Allow domain-owned complex `*Data` DTO reuse only if its full graph passes raw-type validation **and** stable storage naming/null/codec policy matches. Otherwise generate a dedicated storage DTO. No “domain DTO” exemption to recursive checking. Permit `byte[]` only as an explicit binary storage scalar with a defined codec; reject arbitrary `object`, unbounded polymorphism, dictionaries and unsupported collection types in the initial generator. Add support only with a schema/translation test, not by serializing unknown objects.

The mapping descriptor supplies stable storage names, raw types, codecs, nullability/missing policies, member-ID bindings, equality-query capability and domain-to-record member paths. Cache typed mapping/descriptor instances per aggregate-record pair; no per-row reflection or aggregate→BSON→record→BSON→aggregate round trip. BSON serializes plain records only.

### Trusted restoration

1. Decode BSON into raw records using explicit codec/schema policies.
2. Restore raw IDs/tags/audit exactly through explicit trusted root/member restoration. Constructors/factories for commands are not the persistence boundary.
3. Rehydrate scalar/complex VOs via their existing `Rehydrate` contracts and documented policies, not `Create`/`Update`. Provide generated code inside opted-in partial domain declarations for private state access; handwritten restoration is the escape hatch for non-partial/legacy types.
4. Preserve existing scalar fallback behavior initially; do not silently change substitute-vs-preserve semantics. Required complex DTO fields currently reject missing/null: keep that behavior unless an explicit reviewed per-property legacy policy permits otherwise.
5. Never run async command policies, mark audit, regenerate IDs/tags, emit events or write on read. Existing stored representable-invalid values must still be projectable/writeable under the established per-property policy.

| Stored state | Required behavior |
|---|---|
| Missing field | Descriptor-level missing policy; optional absence or explicit legacy/default policy, not automatic constructor defaults |
| Explicit BSON null | Apply declared null policy independently of missing policy |
| Raw value representable but invalid under current business rules | Existing VO rehydration policy; no current command validation |
| Malformed/unrepresentable raw value | Diagnostic failure naming schema/property/reason, without sensitive values |
| Missing tenant/id/audit/tag | Explicit migration error or reviewed historical adapter; never invent security/identity state |

Plain nullable POCO properties cannot distinguish absent from explicit null on their own. The BSON record decoder must track field presence in an external decode context/presence map and apply descriptor policies **before** restoration; do not hide presence flags or BSON types in the normal stored graph. Generated DTO defaults are not missing-field policy.

### Registration and validation

Touch [MongoDatabaseConfiguration](../../src/Synonms.Structur.Infrastructure.MongoDb/Hosting/MongoDatabaseConfiguration.cs), [AggregatePersistenceConfiguration](../../src/Synonms.Structur.Infrastructure/Persistence/AggregatePersistenceConfiguration.cs), [MongoDbRecordMapper](../../src/Synonms.Structur.Infrastructure.MongoDb/Aggregates/MongoDbRecordMapper.cs) and both aggregate repositories.

- Normal aggregate→collection registration resolves generated descriptor/record first or an explicitly registered manual pair; conflicting mappings fail, rather than selecting discovery order.
- The existing `Dictionary<Type,string>` normal constructor becomes descriptor-resolving, a deliberate breaking behavior for callers relying on direct persistence.
- Offer separately named `UseLegacyDirectAggregatePersistence(...)`, marked obsolete with migration guidance, only for explicit legacy deployments. Never select it because generation/mapping is missing.
- At startup validate manual/generated record graphs recursively, including inherited and nested members; reject forbidden types, cycles/unsupported polymorphism, duplicate storage names, missing restoration, incompatible codec or inaccessible bindings. Diagnostics include aggregate/record/property path only.
- No required BSON attributes on domain or records: driver class maps/conventions are built from schema descriptors. Explicit storage overrides take precedence and are validated before serializer resolution.

### Codec compatibility inventory

Before replacing each mapping, record golden BSON for field names/casing/order where contractually significant, discriminators, omitted-vs-null fields, GUID representation, integer widths, decimal/Decimal128 precision, `DateOnly` and `DateTime` shape/time-zone rules. Test both old document→new record→domain and new record→old-compatible BSON. Normalize query constants with the **same** codec, including nullable and enum constants. Do not assume Mongo's default `DateOnly`/decimal serializers reproduce the existing VO serializer output.

Keep scalar/complex VO serializers for the explicit legacy/direct route and other existing callers. The plain-record query path does **not** require VO BSON serializers: it unwraps domain query operands through mapping metadata and raw codecs.

## 2. Bounded source generation after manual parity

### Opt-in and generated artifacts (proposed)

Use an explicitly opted-in `partial` aggregate/member and an explicit persisted-state whitelist. For example, proposed `[GenerateStorage("employee", SchemaVersion = 1)]`, `[StoredState("Forename")]` and `[NotStored]` metadata. Names are stored field names, not inferred migration instructions. Require all candidate state properties to be explicitly included or excluded on opted-in types; computed properties such as `FullName` must be excluded, never picked merely because they are public/readable.

Generate, deterministically:

1. Plain root and nested member/VO storage DTOs, with raw inherited identity/audit/tag fields and explicit nullable/list shape.
2. Typed capture/restore bridges, including partial private-state access and nested `Rehydrate` calls.
3. A stable schema descriptor and generated registration helper, plus domain-expression member-path/codec metadata.
4. Explicit override hooks for custom raw type/codec, renamed historical fields and manual member restoration; no generated business validation.

Bound v1 to concrete root/member types, supported scalar/complex VO contracts, explicit raw scalars, nullable properties, and owned `List<T>`/read-only domain list views backed by supported list state. Require an explicit mapping for unsupported inheritance/polymorphism, ambiguous VO raw types, custom equality or unsupported collections. Existing non-partial types remain usable through handwritten records/mappings.

### Diagnostics and wiring

Reserve proposed diagnostic IDs with focused messages: `STOR001` unsupported/forbidden graph type; `STOR002` unclassified or computed state; `STOR003` ambiguous/colliding storage name; `STOR004` inaccessible/non-partial restoration; `STOR005` missing rehydration or null policy; `STOR006` unsupported collection/override; `STOR007` schema contract conflict. Include source locations and a manual-mapping remedy. Treat correctness diagnostics as compile errors, not silent runtime fallbacks.

Add proposed `src/Synonms.Structur.Generators/` only after the runtime foundation works. The [existing draft](simplified-value-objects-07-source-generation.md) remains separate: this generator is for storage mirrors and typed bridges, not generated VO rules or a guessed command constructor.

- Use an incremental Roslyn generator; proposed analyzer target `netstandard2.0` for compiler-host compatibility. Confirm supported compiler API level against the pinned .NET 10 SDK before selecting a Roslyn package version.
- Add Roslyn versions centrally in [Directory.Packages.props](../../Directory.Packages.props), generator/fixture projects to [the solution](../../Synonms.Structur.slnx), and analyzer project references with `OutputItemType="Analyzer"` and `ReferenceOutputAssembly="false"` in consuming fixtures/Sample.
- Package the analyzer under `analyzers/dotnet/cs`; runtime metadata belongs to Domain/Infrastructure packages, not the analyzer assembly. Do not introduce a runtime dependency on Roslyn.
- Keep generated files compiler outputs under `obj`; stable hint names, ordinal ordering, no timestamps/machine paths/random IDs. Test repeat-build output and incremental unrelated edits.
- Verify a packaged consumer can generate root/member/complex graphs and use a manual override without a project-reference-only accident. No AOT/trimming claim.

## 3. Queries: exact replacement, not in-memory LINQ

### Proposed repository surface

Replace the existing predicate-based `AnyAsync`/`FindFirstAsync`/`ListAsync` overloads, synchronous `Query(): IQueryable<TAggregate>` surface and in-memory paging/selection implementations on `IReadAggregateRepository<TAggregate>` with the following **proposed** cancellation-aware operations. Retain identity lookup and its scoped filtering; preserve existing repository fault/result conventions around these signatures where needed:

```text
AnyAsync(AggregateQuery<TAggregate>, CancellationToken) -> bool
FindFirstAsync(AggregateQuery<TAggregate>, CancellationToken) -> TAggregate?
ListAsync(AggregateQuery<TAggregate>, CancellationToken) -> IReadOnlyList<TAggregate>
CountAsync(AggregateQuery<TAggregate>, CancellationToken) -> long
SelectAsync<TOut>(AggregateQuery<TAggregate>, Expression<Func<TAggregate,TOut>>,
                  CancellationToken) -> IReadOnlyList<TOut>
```

`AggregateQuery<TAggregate>` is an immutable description containing an optional aggregate-typed predicate, ordered sort keys/directions, nonnegative skip and bounded take. Builders append expressions but cannot execute or enumerate. No `IQueryable` escapes, so provider ownership/lifetime is unambiguous. Repository execution owns driver cursors and disposal, propagates cancellation and returns materialized bounded results. Async paging returns items plus total **only when requested**; total uses scoped filter without pagination. A separate count and page are not a snapshot guarantee; document concurrent-change behavior.

Use the same surface in API collection/lookup/projection-support reads. Page by explicit sort plus ID tie-breaker (default ID order); reject invalid/oversized paging. `SelectAsync` projects server-side raw values/DTO construction only, never invokes arbitrary constructors/business functions on the server or fetches entire aggregates to run selectors. Returning full aggregates uses typed restoration after the server query.

### Translation support contract

Translate aggregate expressions to `TRecord` driver expressions/filters through declared paths. Use a typed `IMongoCollection<TRecord>` behind a closed generic adapter selected at registration. Driver `IQueryable<TRecord>` may be an internal implementation detail only; do not expose it or assume every driver LINQ node is supported.

| Expression | V1 behavior |
|---|---|
| `&&`, `||`, `!`, scalar equality/inequality and ordered numeric/date comparison | Supported for declared codec-compatible paths and normalized constants |
| `Id`, `TenantId`, `EntityTag`, scalar VO `.Value`, known outward VO conversions | Rewrite to raw path/type; strip only registered conversion operators |
| Scalar VO equality/known `Equals` | Raw equality only where descriptor asserts exact semantic equivalence; type mismatch cannot become equal |
| Nested member paths, nullable `HasValue`/`Value`, enum/date constants | Supported with declared storage/null semantics; test missing-vs-null behavior explicitly |
| Lists: `Any(element predicate)`, scalar `Contains`, approved constant-set membership | Translate to element-scoped filters; preserve same-element semantics rather than independent dotted matches |
| Sort and raw scalar/member `Select`, simple DTO projection | Server-side with mapped fields and whitelisted conversions |
| Complex VO/object/list structural equality | Unsupported initially unless explicit semantic expansion is registered; never default BSON document equality |
| Arbitrary methods, delegate `Invoke`, compiled predicates, culture-dependent comparison, custom operators or unmapped computed fields | Reject with node/property diagnostic before query execution |

Evaluate only safe captured constants/immutable scalar closure values once at execution and normalize through codecs; do not execute arbitrary captured methods to “simplify” an expression. Specify string comparison as tested ordinal/binary semantics; reject an overload/collation whose CLR meaning cannot be retained. Domain `.Value` paths and raw constants must produce the same BSON fields/types as plain-record queries.

Provide a proposed `IRecordQuery<TAggregate,TRecord>` escape hatch for record-typed filters/sorts/projections when translation is impossible. It is obtained only from the registered aggregate mapping and exposes the same async operations and mandatory scope, **not** `IMongoCollection` or `IQueryable`. An explicit caller migration is required, not automatic fallback.

### Scope and security

Resolve tenant at execution; reject absent tenant in multi-tenant mode. AND the authoritative tenant/deleted scope with the user's predicate for **every** Any/Find/List/Count/Select and raw-record execution. User OR predicates cannot weaken scope. Do not allow public callers to override `TenantId` through the query description. Any privileged include-deleted/history operation needs a separate explicitly authorized internal capability, not a client-controlled flag. Tenant matching also applies to single-ID reads and member/history lookups.

## 4. Ordered work, acceptance and migration

1. Characterize old BSON and query behavior, including current in-memory execution, using framework fixtures; inventory callers before removing sync/`Query`.
2. Add record descriptors/graph validation and typed manual mappings for a minimal framework root/member fixture. Prove restoration and codec parity before touching Sample.
3. Change normal registration to resolve plain records/fail; isolate obsolete direct registration. Replace `MongoDbRecordMapper` on the normal path.
4. Add async query contract, bounded translator, typed provider and scoped raw escape hatch. Migrate framework command/query/lookup callers; remove sync/`IQueryable` APIs in the planned breaking release rather than retain client evaluation.
5. Implement the bounded generator and diagnostics over the proven mapper contract. Verify analyzer packaging and deterministic output.
6. Only then migrate Employee, EmployeeEqualOpportunities, Employment and EmploymentContract storage; retire handwritten mirrors where generated parity is proven.

**Acceptance:** Startup rejects nested forbidden types and missing mappings. Generated/manual routes restore IDs/audit/tags and historical values without command execution or read writes. No normal-path aggregate BSON serialization or VO serializer dependence remains. Unsupported queries fail before database commands; supported queries perform server filters/sorts/pages/counts/projections.

**Targeted validation:** Driver-only fixtures cover graph/mapping/BSON/translation rendering; generator compile/snapshot fixtures cover raw/null/member/override diagnostics and package use. Live replica-set fixtures must capture driver commands/explain representative indexed filters and prove no full collection materialization, correct page/count/select results, list semantics, cancellation and tenant/deleted isolation. Include direct raw-record queries with VO serializer registration deliberately absent. See [the full matrix](framework-usability-06-rollout-validation.md).

**Compatibility:** Repository and normal registration semantics are deliberate source/behavior breaks. Map each removed method to the named async replacement; replace LINQ chains with query description plus Select/Count. Preserve document names/shapes where possible; changed codecs/names require explicit migration/dual-read adapters and a cutover gate. Never dual-write aggregates and records independently. Legacy direct persistence can coexist only as a consciously selected deployment route; generated/manual normal routes must never fall back to it.
