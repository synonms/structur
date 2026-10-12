# Simplified value objects 01: scalar contracts and construction

**Status:** Draft implementation plan  
**Scope:** Establish the shared scalar value-object contract and distinguish validated creation from trusted persistence rehydration. This is the foundation for plans [02](simplified-value-objects-02-json-converters.md), [04](simplified-value-objects-04-mapping.md), [05](simplified-value-objects-05-mongodb-persistence.md), [06](simplified-value-objects-06-formatting.md), and optional [07](simplified-value-objects-07-source-generation.md).

## Goal and context

Scalar value objects currently inherit `SimpleValueObject<TValue>` or a specialization such as `StringValueObject`. Consumers supply constructors, factories, conversions, equality and serialization/mapping support. Establish a discoverable contract that reduces this boilerplate without weakening validation or changing existing behavior.

The most important distinction is between **validated command construction** and **permissive persistence rehydration**. A stored aggregate may predate current business rules. Public creation and updates must use current validation, but loading old state must not be rejected merely because it would fail a current creation rule. Preserve each type's existing `Convert` behavior on initial adoption.

This plan addresses the original request to keep encapsulated, validated value objects while reducing the extra code needed for scalar/complex values, JSON, formatting, mapping and persistence; see the [query](../queries/value-objects.md).

## Scope and non-goals

In scope: scalar contract, validation and rehydration entry points, equality/immutability guidance, conversions, migration of representative built-in types, and contract tests.

Out of scope: JSON/BSON integration (plans 02 and 05), complex-object construction (03), resource/record mapping (04), formatting policy (06), and generated code (07). Do not mass-rewrite every consumer or remove existing public APIs in this stage.

## Existing implementation

- [`SimpleValueObject<TValue>`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/SimpleValueObject.cs) stores `Value` with a protected setter and implements equality with an exact-type check in `Equals(object)`.
- Scalar base classes are in [`ValueObjects/Abstractions`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/); concrete factories typically use `OneOf<TValueObject, IEnumerable<DomainRuleFault>>`.
- [`EmailAddress`](../../src/Synonms.Structur.Domain/ValueObjects/EmailAddress.cs) has `CreateMandatory`/`CreateOptional`; its existing `Convert` validates, then substitutes an empty-string instance on failure.
- Domain rules prefer factory validation; see [Structur coding rules](../../.agents/rules/structur-coding-rules.md) and the [value-object query](../queries/value-objects.md).

## Proposed contract

Add a public generic interface, tentatively:

```csharp
public interface IScalarValueObject<TSelf, TValue>
    where TSelf : IScalarValueObject<TSelf, TValue>
{
    TValue Value { get; }
    static abstract OneOf<TSelf, IEnumerable<DomainRuleFault>> CreateMandatory(
        string propertyName, TValue value);
    static abstract TSelf Rehydrate(TValue value);
}
```

Use the repository's existing `OneOf`/fault types. The exact interface namespace and generic constraints must follow the existing dependency layering. `CreateOptional` remains type-specific where optionality has semantic rules; do not encode optional values as fabricated scalar instances.

`CreateMandatory` is the validated command/API path. `Rehydrate` is a trusted persistence path with no promise that today's creation rules pass. The interface must not supply a generic default implementation that guesses an identity, tenant, security value, or other domain data. A rehydration policy is owned by each concrete type/property and deterministic.

The name `Rehydrate` is preferred over a new stricter meaning for `Convert`. Retain existing `Convert` methods and their exact behavior during rollout, forwarding only when behavior is demonstrably identical. Where a type's current `Convert` already supplies a viable fallback, preserve that behavior and document it. Where it preserves the raw representable value, keep doing so. Any change from substitute to preserve (or vice versa) is a separately reviewed data-compatibility change.

Make scalar `Value` immutable to ordinary callers. Removing the protected setter is a source-compatibility change; first search built-in and consumer patterns and either preserve a compatibility bridge or stage the restriction for a major version. Maintain equality as exact concrete type plus scalar equality. Preserve current outward implicit conversion only where already supported or explicitly adopted; incoming scalar conversion should be explicit so unvalidated values cannot enter command paths unnoticed. C# user-defined casts are convenience syntax, not JSON or BSON configuration.

## Implementation sequence

1. Inventory all scalar base classes, concrete factories, `Convert` methods, implicit operators and tests. Record current invalid-input behavior per concrete type before changing shared behavior.
2. Add the interface and compile-time contract tests. Keep the existing abstract bases usable and avoid a Domain dependency on JSON or MongoDB.
3. Implement the interface on a small representative set (a constrained string type, `EmailAddress`, and one date/numeric type). Each `Rehydrate` implementation must state what happens to representable invalid values.
4. Preserve existing `CreateMandatory`, `CreateOptional`, `Convert` signatures, return types and semantics. Add forwarding only where tests prove equivalence.
5. Migrate additional built-in types incrementally, retaining adapters for types that cannot yet implement the static contract.
6. Document implementer expectations beside the interface and in the relevant value-object skill/reference documentation if one exists.

The cross-plan order is **01 → 02**, **01 → 03 → 04 → 05**; complete 02 and 03 before integrating both API and persistence paths through 04/05. Plan **06** may follow 01 immediately and run independently of 02–05. Plan **07** is optional and should follow stable contracts/adapters, not block any earlier plan.

## Acceptance criteria and targeted regression verification

- A new scalar type can expose its underlying value and a validated mandatory factory through one common contract.
- Validation faults remain the existing `DomainRuleFault` sequence; neither construction nor conversion silently turns validation failure into command success.
- Persistence code can call `Rehydrate` without invoking current creation validation.
- For each migrated type, tests cover valid creation, invalid creation/faults, null/optional policy where applicable, legacy `Convert` behavior, rehydration of representable values outside current rules, immutability expectations and exact-type equality.
- Existing callers of factory and conversion methods still compile; outward cast behavior is unchanged unless explicitly versioned.
- Run focused tests in [`Synonms.Structur.Domain.Tests.Unit`](../../tests/Synonms.Structur.Domain.Tests.Unit/) and build the Domain project. No JSON/BSON tests are required in this stage.

## Compatibility, rollout and pitfalls

Land the contract additively before converting serializers or mapping. Do not make `Convert` stricter, route persistence reads through `CreateMandatory`, or catch factory faults and hide them. Keep `CreateMandatory` as the sole default for public, untrusted input. A domain-only rehydration sentinel may intentionally fail current creation validation; never treat that as a valid command-created value.

Rehydration defaults must be explicit per type/property, deterministic and observable by the persistence boundary. Do not create generic empty IDs, tenant IDs, audit actors, entity tags or access-control values. Persistence defaults for missing fields belong to record/aggregate rehydration, because absent BSON fields do not invoke scalar serializers.
