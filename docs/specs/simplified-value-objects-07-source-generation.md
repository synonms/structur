# Simplified value objects 07: optional source generation

**Status:** Draft implementation plan (optional follow-on)  
**Scope:** Optionally generate repetitive scalar/complex adapter and registration code after the runtime contracts stabilize. Depends on [plan 01](simplified-value-objects-01-contracts.md); useful integration targets are [02](simplified-value-objects-02-json-converters.md), [03](simplified-value-objects-03-complex-value-objects.md), [04](simplified-value-objects-04-mapping.md), [05](simplified-value-objects-05-mongodb-persistence.md) and [06](simplified-value-objects-06-formatting.md).

## Goal and context

Reduce repetitive value-object implementation and registration for Structur itself and consuming projects. Generation is optional: runtime contracts and adapters must remain fully usable without a generator. Generated code must not own or invent domain validation or persistence fallback policy.

This is the optional boilerplate-reduction follow-on from the original request to make value objects simpler for framework and consumer authors; see the [user story](../user-stories/value-objects.md).

## Scope and non-goals

In scope: an incremental Roslyn generator and analyzer diagnostics for explicitly opted-in scalar/complex types; generation of boilerplate wrappers, operators, equality/formatting support where safe, and serializer/mapping metadata.

Out of scope: generating business rules, guessing required-field defaults, replacing validated factories, a generic source-generated aggregate constructor, EF Core, or claiming AOT/trimming support without dedicated evidence.

## Existing implementation

- Scalar base and proposed common contract are described in [plan 01](simplified-value-objects-01-contracts.md).
- Existing JSON, mapping and Mongo registration entry points are [`DefaultResourceJsonConverter<TResource>`](../../src/Synonms.Structur.Api.Core/Serialisation/Default/DefaultResourceJsonConverter.cs), [`MappingHelper`](../../src/Synonms.Structur.Api.Server/Mapping/MappingHelper.cs) and [`StructurBsonSerialisation`](../../src/Synonms.Structur.Infrastructure.MongoDb/Serialisation/StructurBsonSerialisation.cs).
- Current consumer projects target .NET 10; inspect the actual SDK/package conventions before choosing generator target frameworks and analyzer packaging.

## Proposed design

Opt in using a single partial declaration and explicit scalar underlying type/contract, with handwritten validated factory and rehydration methods retained as the authority. The generator may provide boilerplate value access wrappers/operators, exact-type equality where safe, invariant formatting hooks, converter/codec metadata and registration helpers. It must call the handwritten strict factory for API input and the separate handwritten `Rehydrate` method for persistence.

For complex values, generate only adapters from explicit member/factory metadata. Do not synthesize mandatory/missing defaults, infer validation rules from attributes, or mutate private state. Unsupported members, ambiguous constructors, missing strict factory, missing rehydration policy, inaccessible partial declarations and invalid underlying types produce actionable compile-time diagnostics; no silent generated fallback.

Generated JSON/BSON metadata must preserve API strictness, persistence permissiveness, primitive shapes, null/missing distinctions and custom serializer precedence. The runtime reflection/registration path remains viable for projects that do not enable generation. Static abstract interfaces alone are not evidence of reflection-free or trimming-safe operation.

## Implementation sequence

1. Defer until plans 01–06 establish stable runtime contracts and tests; identify boilerplate that remains materially repetitive.
2. Define minimal opt-in syntax and diagnostics before writing generation logic. Keep business factory and per-property rehydration policy handwritten.
3. Add the generator project/package only if value warrants it, following existing package and solution conventions.
4. Generate one scalar consumer fixture and one unsupported-type diagnostic fixture; compare generated behavior with runtime fallback.
5. Add generated registration for relevant adapters without taking control away from explicit consumer registrations.
6. Add a consumer compile fixture that references the published analyzer/package as a consumer would. Test build output and packaging.
7. Test trimming/AOT only if support is an explicit goal; otherwise document the limitation rather than claiming compatibility.

## Acceptance criteria and targeted regression verification

- A consumer can opt in with one partial declaration while retaining handwritten validation and persistence rehydration policies.
- Generated strict JSON/API construction cannot bypass current validation; generated persistence access cannot accidentally call that strict path.
- BSON primitive representation, query translation and explicit serializer precedence match runtime adapters.
- Unsupported or ambiguous declarations yield focused compiler diagnostics, not malformed generated code or permissive fallback.
- A consumer-project compile fixture builds with generation enabled; existing non-generated consumer/runtime fallback continues to work.
- Run generator unit tests and consumer fixture builds using the selected existing test infrastructure; build all directly affected projects. Do not add a new testing tool solely for this plan.

## Compatibility, rollout and pitfalls

This is optional and last in the implementation order; do not block adoption of runtime contracts on it. Keep generated APIs additive and versioned with the analyzer package. Do not generate invalid-value substitutions or fabricate identity, tenant, audit or security values. Preserve the HTTP-vs-persistence boundary and logging restrictions. Do not advertise trimming/AOT support unless generated and non-generated paths are both tested under those deployment modes.
