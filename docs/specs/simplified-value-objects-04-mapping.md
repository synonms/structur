# Simplified value objects 04: resource and record mapping

**Status:** Draft implementation plan  
**Scope:** Reduce fragile reflection-based scalar and complex value-object mapping through shared contracts and cached runtime mapping plans. Depends on [plan 01](simplified-value-objects-01-contracts.md); coordinate persistence boundaries with [plan 03](simplified-value-objects-03-complex-value-objects.md) and [plan 05](simplified-value-objects-05-mongodb-persistence.md).

**Implemented complex subset:** Plan 03 now supplies `IComplexValueObject<TSelf,TData>` and cached `ToData`/DTO-to-resource output plans in `ComplexValueObjectResourceMapping`, integrated into the default root/child mappers and `MappingHelper`. Built-ins need no handwritten adapters; explicitly registered output adapters remain available for exceptional shapes. Scalar mapping-plan unification and a total mapper rewrite remain deferred. API resource input and BSON record round-trip mapping are unchanged.

**Nested adapters:** After `ToData`, nested members are mapped using their declared raw DTO types. An `Address -> AddressResource` adapter applies when mapping an `Address` directly, not when a parent DTO contains `AddressData`. Register `ComplexValueObjectResourceMapping.Register<AddressData, AddressResource>(...)` for that nested path. Adapters are checked on each mapping invocation and take precedence even after a conventional plan has been cached. Prefer startup registration for consistent request behavior.

**Collections:** The complex mapper resolves the actual `IEnumerable<T>` contract (including inherited interfaces and non-generic classes derived from generic lists), rather than assuming the first generic argument is the element type. Lists, compatible collection interfaces and vector arrays preserve order and null elements and produce fresh output collections. Ambiguous or non-generic enumerable shapes, non-vector arrays, and arbitrary destination collection construction require explicit adapters; they are not treated as DTO property bags. Strings remain scalar.

## Goal and context

Reduce custom mapping code between aggregates and public resources/records while preserving observable API behavior. Reflection `SetValue` does not invoke C# implicit operators, so operators alone cannot make mapping work. API-to-domain mapping must remain validated; record-to-aggregate mapping is trusted, permissive rehydration.

This is the mapping portion of the original request to simplify use of value objects in Structur and consumer projects without losing domain rules or API behavior; see the [user story](../user-stories/value-objects.md).

## Scope and non-goals

In scope: scalar unwrapping and compatible destination assignment, complex/nested/collection mapping, enum special case, diagnostics, cached plans and regression fixtures.

Out of scope: changing hypermedia/routing/version/schema behavior, changing request resource contracts, switching persistence away from BSON round-trip in this stage, silently coercing incompatible types, or generating mapping code as a prerequisite.

## Existing implementation

- [`DefaultResourceMapper`](../../src/Synonms.Structur.Api.Server/Mapping/DefaultResourceMapper.cs) dispatches among mapped property categories.
- [`MappingHelper`](../../src/Synonms.Structur.Api.Server/Mapping/MappingHelper.cs) unwraps scalar values reflectively, handles string scalar-to-enum parsing, maps nested complex resources and collections, and assigns properties reflectively.
- [`DefaultResourceMapperTests`](../../tests/Synonms.Structur.Api.Server.Tests.Unit/Mapping/DefaultResourceMapperTests.cs) covers aggregate-to-resource output.
- [`MongoDbRecordMapper`](../../src/Synonms.Structur.Infrastructure.MongoDb/Aggregates/MongoDbRecordMapper.cs) currently serializes source to BSON then deserializes into destination. Records are not primitive-only: [`EmployeeRecord`](../../src/sample/Synonms.Structur.Sample.Api/Features/Employees/Persistence/EmployeeRecord.cs) has nullable domain value objects and complex values.
- The record design and nullable/defaulted record convention are in [`persistence-records.md`](persistence-records.md).

## Proposed design

Introduce a shared mapping-plan abstraction with cached, deterministic property bindings built per source/destination type pair. For scalar bindings, use plan 01's value contract/accessor and explicit compatibility rules. Preserve the existing string-scalar-to-enum mapping (including exact parsing policy), null handling and property naming. The complex subset is implemented through plan 03's domain-owned DTO contract, cached typed projections and conventional nested/collection bindings; custom adapters are optional escape hatches, not required per built-in.

Retain reflection as an implementation strategy where needed, but do not rescan metadata for every object or depend on implicit operators. Resolve plans once, validate each binding, and surface a typed mapping diagnostic for incompatible property types. Do not silently drop a matched but incompatible value. Existing intentional unmatched properties remain governed by current mapper semantics; characterization tests decide where diagnostics are appropriate.

Keep the existing persistence BSON round trip untouched while the shared plan is introduced. Replacing it is a later measured change only after equivalent record defaults, nested serializers, legacy shapes, query behavior and error behavior are demonstrated. Do not claim current records are raw primitive DTOs.

Maintain separate entry points/modes:

- **API/domain command path:** resource DTO deserialization followed by current aggregate/value-object factories and fault aggregation. Mapping a resource must not call permissive rehydration.
- **Persistence path:** record-to-aggregate mapping applies explicit per-type/property permissive rehydration policies, not today's creation validation. Reverse mapping writes the current in-memory values without validating unchanged legacy values.

Preserve mapper behavior outside value objects: links/hypermedia, route generation, schema/version filtering, entity identifiers, child resources, and the current API enum handling.

## Implementation sequence

1. Characterize current `DefaultResourceMapper` tests and inspect the remaining dispatcher/property matching behavior before refactoring.
2. Add cached mapping-plan construction and focused scalar plans; preserve public mapper constructors/interfaces.
3. Add explicit collection, nullable, enum, nested-complex and incompatible-type handling. Complex output already uses the distinct DTO contract and cached plans from plan 03; retain optional typed adapters for noncanonical shapes.
4. Add diagnostics through the existing mapper/logger mechanism without leaking sensitive values. Do not log raw scalar contents.
5. Keep Mongo's BSON round trip. Add record/aggregate fixtures documenting mapping assumptions and missing-field/default handling; move to a replacement only in a separate parity-reviewed change.
6. Compare performance/metadata work only if needed; generated assignment code is optional plan 07.

## Acceptance criteria and targeted regression verification

- Existing aggregate-to-resource results remain the same for scalar properties, collections, enums, nested resources, links and route data.
- Scalar values map to compatible primitive destinations without relying on casts; compatible scalar-to-scalar and complex nested bindings have explicit tested rules.
- Null, nullable collections, empty collections and mismatched property types are covered. Incompatible matched assignments surface a diagnostic rather than silently disappearing.
- No resource mapping path uses persistence `Rehydrate`; no record mapping path validates a legacy aggregate against today's creation rules.
- Record/aggregate mapping tests include the actual nullable value-object record shape used by `EmployeeRecord`.
- Run API Server unit tests, especially [`DefaultResourceMapperTests`](../../tests/Synonms.Structur.Api.Server.Tests.Unit/Mapping/DefaultResourceMapperTests.cs), plus the focused MongoDB/sample persistence test home if changed. Build API Server and MongoDB.

## Compatibility, rollout and pitfalls

Implement after scalar contracts; complex mapping depends on plan 03. Preserve all non-VO mapping behavior and public interfaces. Reflection and implicit casts are not interchangeable. Avoid silent “null on mismatch” fallbacks. BSON round-trip mapping stays until parity is explicitly measured and approved; an apparent simplification must not drop legacy BSON compatibility, record initializers or serializer/query metadata.

For persistence, distinguish missing from explicit null and representable invalid legacy values from malformed structures. Missing BSON fields are not passed through scalar serializers: record initializers or aggregate rehydration mapping must apply the documented property policy.
