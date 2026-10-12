# Simplified value objects 03: complex value objects

**Status:** Draft implementation plan  
**Scope:** Define immutable construction and explicit mapping/rehydration contracts for object-shaped value objects, including nested scalar values. Depends on [plan 01](simplified-value-objects-01-contracts.md); the scalar JSON converter in [plan 02](simplified-value-objects-02-json-converters.md) is a building block, not a prerequisite for persistence.

## Goal and context

Support value objects containing several meaningful properties without weakening validation or hiding compatibility behavior behind reflection. Keep API object shape and existing aggregate validation while making nested values easier to serialize and map. Persistence must be able to load legacy graphs permissively, independently of validated public construction.

This addresses the complex-value-object portion of the original request: retain value semantics and encapsulation while reducing bespoke serializer/mapping work; see the [user story](../user-stories/value-objects.md).

## Scope and non-goals

In scope: complex value-object construction contracts, exact-type/value equality, nested scalar handling, API-vs-persistence construction paths and representative built-in coverage.

Out of scope: generic reflection that mutates private backing fields, making all public constructors validating, changing resources into domain objects, replacing MongoDB's BSON round-trip mapper before parity is established, or requiring source generation (plan 07).

## Existing implementation

- [`ComplexValueObject`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/ComplexValueObject.cs) compares atomic values with `SequenceEqual`, but currently does not check exact runtime type in `Equals(object)`.
- [`Address`](../../src/Synonms.Structur.Domain/ValueObjects/Address.cs) has a private constructor, private setters, nested scalar properties and validated `CreateMandatory`/`CreateOptional` factories.
- [`DefaultResourceJsonConverter<TResource>`](../../src/Synonms.Structur.Api.Core/Serialisation/Default/DefaultResourceJsonConverter.cs) deserializes properties into declared resource types. Domain factories currently perform API-to-domain validation.
- MongoDB registration and BSON class maps are in [`StructurBsonSerialisation`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/StructurBsonSerialisation.cs); persistence mapping details are in [plan 05](simplified-value-objects-05-mongodb-persistence.md).

## Proposed design

Keep complex construction type-owned. Do not make an arbitrary public constructor or record `init` setter a substitute for validated factory construction: object initializers/`with` can otherwise bypass invariants. Do not add a generic private-field mutation mechanism.

Use explicitly registered, strongly typed adapters; do not attempt to force variable-shaped factories into a reflection-inferred universal constructor. Define an `IComplexValueObjectAdapter<TValueObject, TResource>` per resource/domain pair with a typed `CreateMandatory(propertyName, resource)` returning the existing `OneOf<TValueObject, IEnumerable<DomainRuleFault>>`, and a cached ordered set of member bindings for output mapping. Define a separate `IComplexValueObjectRehydrator<TValueObject, TPersistedShape>` per persisted/domain pair with a `Rehydrate(persistedShape)` method. Register adapters in the API/mapping or persistence composition root, keyed by the concrete source/destination types. Keep both contracts and metadata free of STJ and BSON types; adapters may live in the layer that already references those packages. An adapter implementation can call existing factories and existing per-type conversions, but API construction and trusted rehydration must never share a fallback path.

Each adapter owns and exposes metadata that can:

- construct from a typed API/resource-shaped input by invoking the existing validated factory (or an equivalent typed factory);
- construct from persistence data through a separately named trusted rehydration method, preserving the type's existing conversions/default policy;
- enumerate readable members and map member values without changing external resource naming or validation behavior.

The first implementation must prove this runtime integration without a generator. Cache adapter/member metadata at registration/plan creation. The adapter—not generic reflection—owns conversions and factory invocation. If an existing type cannot provide a meaningful rehydration policy, retain its current persistence mechanism and record the limitation rather than using validated creation as a fallback.

These handwritten adapters are an intermediate compatibility mechanism, not the final "one value-object declaration" authoring experience. Reuse conventional readable-member bindings for output and keep type-owned factories/default policies on the value object; adapters should only connect existing contracts. Plan 07 can generate this connection metadata for consumer types. Do not require consumers to implement new JSON or BSON parsers merely because their object has multiple properties.

The API resource remains a separate DTO. Deserializing a complex resource does not itself imply an aggregate has passed validation; aggregate `Create`/`Update` continues to collect current domain-rule faults. For trusted persistence, define a per-property policy for missing, explicit null, invalid-but-representable and malformed/unrepresentable members. Optional missing/null values normally remain absent; required missing values use explicit per-type/property rehydration policy. Preserve each existing `Convert` behavior during initial rollout.

Complex equality must be exact concrete type plus ordered atomic-value equality. For collection-valued members, explicitly choose sequence equality and matching hash behavior; do not assume autogenerated record equality is equivalent.

## Implementation sequence

1. Inventory complex value objects, their factories, constructors, setters, atomic-value definitions and Mongo class-map behavior. Start with `Address`; inspect actual deserialization behavior before changing visibility or serializer conventions.
2. Define a typed adapter contract and separate API factory from trusted persistence factory. Keep adapter registration in a layer that can reference Domain and the relevant serializer/mapping APIs.
3. Correct `ComplexValueObject` exact-type equality and collection hash/equality only with characterization tests to prevent unrelated behavior changes.
4. Implement adapters for a nested object and a collection-bearing object. Ensure nested scalar factories on the API path are validated and rehydration remains permissive.
5. Integrate API serialization/mapping where necessary, preserving resource DTOs and normal aggregate validation.
6. Integrate persistence only after recording old BSON shapes and each field's existing `Convert` behavior; do not replace the current BSON record round trip without parity evidence.

## Acceptance criteria and targeted regression verification

- API construction invokes current validated complex factories; invalid members produce the existing domain faults through the existing aggregate/resource path.
- Trusted persistence construction does not invoke the API validation factory and preserves current per-type conversion behavior.
- Tests cover nested valid/invalid scalar values, optional members, collection ordering/equality, exact-type equality, and missing/null/invalid-representable/malformed member cases.
- `Address` retains its API/resource object shape and private construction boundary; no generic private-member mutation is introduced.
- Run focused Domain unit tests and add focused API Server tests for complex-resource mapping/serialization. Build Domain, API Server and MongoDB if its adapter surface is touched.

## Compatibility, rollout and pitfalls

Implement after scalar contracts; generator support is optional and later. Keep validated creation and trusted persistence as visibly separate calls. A missing required property is not the same as explicit JSON/BSON null, a representable legacy value outside current validation, or a structurally malformed value. A viable sentinel may be domain-owned even if it fails today's creation rules; it must not be used for new API commands.

Persistence reads must not mark audit state, mutate entity tags, emit events or rewrite records. Diagnostics may report that a substitution occurred, but must exclude raw sensitive values. Broad catch-and-default behavior is prohibited; corrupt/unrepresentable shapes need a surfaced, diagnostic error.
