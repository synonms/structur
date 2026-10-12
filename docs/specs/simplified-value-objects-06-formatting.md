# Simplified value objects 06: invariant formatting

**Status:** Draft implementation plan  
**Scope:** Establish predictable display formatting for scalar value objects without changing wire or persistence formats. This plan depends only on [plan 01](simplified-value-objects-01-contracts.md) and can be implemented immediately after it, independently of plans 02–05.

## Goal and context

Provide sensible `.ToString()` and `IFormattable` defaults for scalar values: culture-independent date/numeric output and a consistent override point for domain-specific display. Formatting is a display concern and must never redefine JSON/BSON serialization or mutate/round the stored value.

This is the display-formatting portion of the original request to reduce repeated value-object boilerplate while preserving their domain-specific meaning; see the [user story](../user-stories/value-objects.md).

## Scope and non-goals

In scope: default scalar formatting, format/provider overloads, explicit date/time policies, decimal display defaults and override tests.

Out of scope: changing wire formats, rounding values, localization UX, changing complex-object `ToString()` semantics, or assuming collection formatting/equality.

## Existing implementation

- [`SimpleValueObject<TValue>`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/SimpleValueObject.cs) exposes `Value` but does not define formatting.
- Scalar specializations are under [`ValueObjects/Abstractions`](../../src/Synonms.Structur.Domain/ValueObjects/Abstractions/).
- [`Units`](../../src/Synonms.Structur.Domain/ValueObjects/Units.cs) and other domain-specific types may require display precision different from a generic decimal default; inspect each override before migration.
- Formatting must remain separate from JSON and MongoDB, covered in [plan 02](simplified-value-objects-02-json-converters.md) and [plan 05](simplified-value-objects-05-mongodb-persistence.md).

## Proposed design

Implement `IFormattable` behavior in the shared scalar base when `TValue` supports formatting, while retaining virtual `ToString()` and a virtual format policy for concrete overrides. The default without an explicit provider is invariant culture. An explicit `IFormatProvider` is honored for callers that deliberately request localized presentation. Unsupported formats use the underlying framework type's normal format exception; do not silently switch formats.

Recommended defaults:

| Underlying type | Default display |
| --- | --- |
| `DateOnly` | `yyyy-MM-dd` |
| `DateTimeOffset` | round-trip `"O"` |
| `DateTime` | round-trip `"O"`; preserve the original `Kind` and do not invent UTC |
| `decimal` | `"F2"` as display-only policy; concrete type may override |
| integral types | invariant general numeric representation |
| `double` | invariant round-trip `"R"` |
| `string` | exact underlying string |

The default `ToString()` should use invariant culture and the type policy. Explicit format/provider calls follow `IFormattable`. `F2` must not round the underlying `Value`, affect equality, or be reused as a JSON/BSON encoding. Types whose domain display needs more precision (including `Units` if applicable) override the display policy. Do not apply scalar formatting to complex value objects; their display is type-owned. Collection sequence equality/formatting remains explicit.

## Implementation sequence

1. Inventory existing `ToString`, `IFormattable` and culture assumptions for scalar types; record callers whose output may be persisted or compared.
2. Add the shared formatting implementation without removing concrete overrides.
3. Add per-underlying-type format policies, including date/time and decimal; verify `DateTime.Kind` preservation.
4. Add domain-specific overrides where the generic policy is not a correct display for that concept.
5. Ensure serializers and mappers use `Value`/their explicit codecs rather than `ToString()` for wire or persistence representations.

## Acceptance criteria and targeted regression verification

- Scalar default formatting is invariant and matches the table; explicit format/provider overloads behave according to `IFormattable`.
- `DateTime` output does not silently change `Kind` or assign a timezone; `DateTimeOffset` remains round-trip.
- Decimal display to two places does not alter stored precision, equality, JSON or BSON output; domain types can override.
- Existing type-specific display contracts are retained or explicitly documented as intentional changes.
- Complex value object equality remains independent of formatting; no automated collection `ToString()` is introduced.
- Add focused formatting tests to [`Synonms.Structur.Domain.Tests.Unit`](../../tests/Synonms.Structur.Domain.Tests.Unit/) and build Domain.

## Compatibility, rollout and pitfalls

Implement after plan 01; no dependency on the other integration plans. Search for callers that relied on culture-sensitive `ToString()` or use its output as a wire/database value. Keep explicit serializers on primitive values and avoid routing all display through formatting. Changes to displayed output can affect logs/UI but must not alter persisted representation.
