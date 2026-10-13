# Resource mapping 01: deterministic cached mapping plans

**Status:** Draft implementation plan  
**Scope:** Make aggregate-to-resource and member-to-child-resource mapping deterministic, diagnosable, and efficient without changing the public API representation. This is the foundation for [multiple resource representations](resource-mapping-02-multiple-representations.md) and [plain resource contracts](resource-mapping-03-plain-contracts.md). It complements, but does not replace, the complex value-object output mapping in [simplified value objects 03](simplified-value-objects-03-complex-value-objects.md).

## Goal and context

The current root and child mappers independently discover destination properties, match source properties by name, classify each destination type, and execute a large type switch for every mapped object. Reflection assignment failures in the root mapper are logged and swallowed; unmatched properties and unsupported shapes can become null without a clear explanation. Mapping behavior and custom mapper selection are consequently difficult to reason about.

Build a reusable mapping plan once per source/destination pair. Keep aggregates as the domain model, resources as external contracts, and retain convention mapping for the common case. Make exceptional mappings explicit, with validation at registration/startup rather than successful-looking partial output.

## Existing implementation

- [`DefaultResourceMapper`](../../src/Synonms.Structur.Api.Server/Mapping/DefaultResourceMapper.cs) maps aggregate roots and creates IDs, self/projection/edit/delete links.
- [`DefaultChildResourceMapper`](../../src/Synonms.Structur.Api.Server/Mapping/DefaultChildResourceMapper.cs) repeats the property dispatch for aggregate members.
- [`MappingHelper`](../../src/Synonms.Structur.Api.Server/Mapping/MappingHelper.cs) handles scalar/value-object unwrapping, lookups, related links, nested resources and collections.
- [`ComplexValueObjectResourceMapping`](../../src/Synonms.Structur.Api.Server/Mapping/ComplexValueObjectResourceMapping.cs) already caches complex output plans and provides explicit typed adapters. Its DTO-to-resource behavior and adapter precedence should be reused or integrated, not duplicated.
- [`ResourceMapperFactory`](../../src/Synonms.Structur.Api.Server/Mapping/ResourceMapperFactory.cs) and [`ChildResourceMapperFactory`](../../src/Synonms.Structur.Api.Server/Mapping/ChildResourceMapperFactory.cs) select the first matching service registration. `AddStructur` registers discovered resource mappers more than once around default mapper registrations, so the intended custom-over-default behavior is implicit in registration order and duplicate registrations can occur.
- [`DefaultResourceMapperTests`](../../tests/Synonms.Structur.Api.Server.Tests.Unit/Mapping/DefaultResourceMapperTests.cs) characterizes a subset of root mapping behavior; add equivalent coverage for child mapping and error cases.

## Scope and non-goals

In scope: cached mapping metadata and executable bindings; scalar, enum, complex, nested-resource, related-link and collection dispatch; explicit override resolution; startup diagnostics; tests for root and child mapping.

Out of scope: changing HTTP routes or resource inheritance, resource-to-aggregate validation, persistence record mapping, changing hypermedia/version behavior, automatic coercion between arbitrary scalar types, and source generation. Preserve the existing mapper interfaces and default output while introducing the plan behind them.

## Proposed design

Introduce an immutable `ResourceMappingPlan` (or equivalent) keyed by the exact source and destination types. A plan contains ordered destination bindings, a source accessor when one exists, the conversion strategy, and any contextual behavior needed for links. Build the plan once, cache it using a thread-safe cache, and execute it for each object. Root and child mappers share the plan-building and property-conversion rules; root/child-specific identity and link setup remains in their respective mapper paths.

Plan construction should:

- Discover public readable source properties and writable destination properties once, excluding framework-owned IDs, links, version metadata, and other properties managed by the mapper.
- Preserve same-name conventions, unmatched destination defaults, scalar value-object unwrapping, entity IDs, enum behavior, lookups, nested resources, child resources, related links, and collection order/null policy.
- Use the existing complex value-object contract and adapter support for projected DTOs, including nested DTO/resource adapters.
- Reject ambiguous source/destination bindings and matched-but-incompatible types with an actionable diagnostic naming the pair and property. Do not log property values.
- Keep a deliberately unmatched destination property unchanged, allowing resource-only fields/defaults, unless an explicit required-binding policy says otherwise.

Provide an explicit mapping configuration/adapter seam for exceptional cases such as renamed properties or custom conversion. Registration must be keyed to the exact pair and property, be deterministic, and take precedence over convention bindings. Avoid a second mapping abstraction that competes with `ComplexValueObjectResourceMapping`; decide whether to generalize that registry or adapt it as a specialized plan strategy.

Resolve mapper overrides explicitly during service registration. Register each discovered implementation once, choose a custom implementation over the generated default by a defined rule, and fail during startup if more than one custom implementation claims the same pair. Apply the same policy to child mappers. Factories should not silently take the first result from `IEnumerable<T>`.

Plan generation must not invoke aggregate constructors or domain validation. API input continues through the existing resource deserialization, factory and aggregate command path. This plan is output-only.

## Implementation sequence

1. Characterize current root/child mapping for scalar and nullable values, enums, value objects, lookups, nested resources, collections, IDs, links, unmatched properties and null values. Record any intentional behavior before refactoring.
2. Correct mapper registration and resolution: remove duplicate assembly scans, establish default/custom precedence, and add startup diagnostics for ambiguous custom registrations.
3. Add immutable cached plan construction for scalar and direct compatible bindings. Keep existing `IResourceMapper` and `IChildResourceMapper` public contracts.
4. Route existing root and child mapping through common plan execution, retaining specialized identity/link creation and preserving complex-value-object adapter behavior.
5. Add explicit pair/property overrides for renamed or custom mappings and validate them when plans are built.
6. Add failure tests for incompatible types, ambiguous overrides and mapper registrations; add regression tests proving errors are not swallowed and no raw property values are logged.
7. Measure plan construction and repeated mapping only if performance is a concern; avoid source generation until the runtime plan and override contracts stabilize.

## Acceptance criteria and targeted regression verification

- Repeated mapping of a source/destination pair reuses one plan; plan construction is thread-safe and does not rescan property metadata per object.
- Existing sample root/child mappings retain their values, null behavior, links, route generation, collection order, IDs, and complex DTO output.
- A custom mapper has one documented precedence rule and is selected exactly once; duplicate or ambiguous custom registrations fail deterministically.
- A matched incompatible binding fails with a diagnostic identifying source type, destination type and property. It is not converted to null or hidden by a catch.
- An unmatched optional/resource-only destination property retains its configured default; explicit property adapters override convention mapping.
- API input remains validated through domain factories; the output plan cannot call permissive persistence rehydration.
- Run `dotnet test --project tests/Synonms.Structur.Api.Server.Tests.Unit/Synonms.Structur.Api.Server.Tests.Unit.csproj --no-restore`, including root/child mapper tests, and build `Synonms.Structur.Api.Server`.

## Compatibility, rollout and pitfalls

Land this as an internal implementation change first. Keep mapper interfaces, routes, wire payloads and registered resource behavior unchanged. Do not broaden scalar coercion beyond tested compatibility rules. Do not convert every missing property to a startup error: unmatched destination members are useful for external-only data; distinguish these from matched-but-incompatible properties and explicitly required bindings.

Mapping adapters must not become a back door for unvalidated request data. Output mapping may project the domain model, while resource-to-domain commands must continue to use the established validation path. Preserve custom mapper selection behavior only where it is unambiguous; make accidental registration-order dependence an explicit diagnostic instead of relying on it.
