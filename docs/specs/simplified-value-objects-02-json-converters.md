# Simplified value objects 02: System.Text.Json generic converters

**Status:** Draft implementation plan  
**Scope:** Replace per-scalar JSON converters/parsers with one registered converter factory for public API JSON. Depends on the scalar contract in [plan 01](simplified-value-objects-01-contracts.md); complex JSON object graphs are addressed in [plan 03](simplified-value-objects-03-complex-value-objects.md).

## Goal and context

Represent scalar value objects as their underlying JSON primitives, not `{ "value": ... }`, while keeping the public JSON boundary validated. Use `System.Text.Json` (STJ) in place of per-type JSON handling. Keep resource parsing, resource-to-domain validation and domain-fault aggregation behavior intact.

This is the JSON slice of the original request to preserve value-object encapsulation and business rules while reducing custom serializer work for Structur and consumers; see the [user story](../user-stories/value-objects.md).

## Scope and non-goals

In scope: a generic `JsonConverterFactory`, public options registration, scalar primitive parsing/writing, nullable values, nested values and collections.

Out of scope: changing request resource shapes, bypassing aggregate validation, using converters for MongoDB rehydration, or adding STJ dependencies to Domain. Persistence JSON, if introduced later, requires explicitly registered trusted rehydration options; it must never be enabled on HTTP/API options.

## Existing implementation

- [`DefaultResourceJsonConverter<TResource>`](../../src/Synonms.Structur.Api.Core/Serialisation/Default/DefaultResourceJsonConverter.cs) delegates property values to `JsonElement.Deserialize(propertyType, options)` and serializes properties through STJ.
- [`Synonms.Structur.Api.Core.csproj`](../../src/Synonms.Structur.Api.Core/Synonms.Structur.Api.Core.csproj) references Core, not Domain. [`Synonms.Structur.Api.Server.csproj`](../../src/Synonms.Structur.Api.Server/Synonms.Structur.Api.Server.csproj) references both.
- Scalar factories and their `OneOf` faults reside in Domain; casts alone are not recognized by STJ.

## Proposed design

Implement `JsonConverterFactory` and a closed generic converter for scalar contract implementations. The factory checks the scalar interface and creates the matching converter. Keep registration in the API layer that owns configured `JsonSerializerOptions` (prefer API Server or an existing serializer-options adapter); do not introduce a Core-to-Domain project reference or put JSON concerns in Domain.

**Read contract (untrusted API boundary):**

1. Read exactly the underlying primitive type using STJ and the active options.
2. Invoke the scalar's validated `CreateMandatory` contract.
3. On parse, token-kind or validation failure, throw `JsonException` with the member path/type context and safe fault descriptions. Do not include sensitive raw input.
4. Preserve existing request-resource parsing and allow its normal validation pipeline to aggregate domain faults. The scalar converter should not fabricate a successful value or replace aggregate validation.
5. Handle JSON `null` at the correct layer. `Nullable<T>` distinguishes optional value types, but nullable reference annotations do not produce different runtime scalar types: a scalar converter alone cannot distinguish `EmailAddress` from `EmailAddress?`. Preserve null for reference scalars at converter level; enforce required/non-null resource members through the existing aggregate validation and, where appropriate, STJ contract metadata with explicitly configured nullable-annotation handling. Missing-member enforcement is separate from null handling. Let STJ nullable wrappers operate normally for value types.

**Write contract:** Write only `Value` through STJ using the primitive type and active options. Never serialize the display `ToString()` representation unless it is also the defined primitive representation. Include tests proving scalar JSON stays a primitive and that primitive request resources retain their current shape.

Register one factory once in each relevant API options instance, before serialization begins. Do not register a trusted/persistence mode on shared HTTP options. Converters must work for direct scalar values, nested resource properties, arrays/lists, and nullable scalars using the same options.

## Implementation sequence

1. Locate all construction/configuration sites for API `JsonSerializerOptions` and `DefaultResourceJsonConverter`; add a registration point without changing unrelated options or serializer implementations.
2. Implement the factory and converter against plan 01's contract. Validate explicitly known registrations during options setup; consumer types first encountered through lazy converter discovery must surface closed-type construction errors on first resolution rather than silently falling back.
3. Implement primitive token handling and map expected format/validation failures to `JsonException`; do not catch arbitrary exceptions broadly.
4. Register in the HTTP/API options path. Keep STJ option instances used for unrelated internal/persistence work unchanged.
5. Add scalar tests for representative string, numeric, date and nullable types, then integration coverage through the default resource converter and any existing Ion paths that consume the same options.
6. Confirm existing version filtering, links, enum string behavior, unknown properties and resource validation remain unaffected.

## Acceptance criteria and targeted regression verification

- Valid JSON primitive input creates the scalar through the validated factory; invalid syntax, primitive type or business value fails with `JsonException`.
- Serialization emits the primitive JSON token with the existing culture-independent wire value, never an object wrapper.
- Nullable scalar `null`, nested scalar properties and scalar collections work correctly. Required-member null or omission is rejected by the configured resource/aggregate boundary, with tests distinguishing nullable reference annotations, `Nullable<T>`, explicit null and omitted members.
- Default resource JSON still maps property names, preserves version/hypermedia behavior and leaves request-resource validation/fault aggregation in its existing layer.
- HTTP options cannot accidentally select permissive `Rehydrate`; any future persistence JSON configuration is a separately named and explicitly registered trusted options instance.
- Run API Server unit tests, especially [`DefaultResourceMapperTests`](../../tests/Synonms.Structur.Api.Server.Tests.Unit/Mapping/DefaultResourceMapperTests.cs) for adjacent mapping behavior, plus focused serialization/resource tests in that project. Build API Core and API Server.

## Compatibility, rollout and pitfalls

Land after plan 01 and before any optional generator. Existing API payloads are primitive scalar forms; preserve them byte-for-byte where practical. Do not change request fields to value-object-shaped objects. Do not infer a permissive converter from `Convert`, and do not add persistence fallback behavior to the public API converter. Ensure errors do not echo potentially sensitive submitted values.
