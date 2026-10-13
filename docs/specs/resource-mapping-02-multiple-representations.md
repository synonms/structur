# Resource mapping 02: multiple representations per aggregate

**Status:** Implemented  
**Scope:** Allow an aggregate to be represented by multiple external resource contracts, such as public/admin views or independently versioned APIs. Depends on [deterministic cached mapping plans](resource-mapping-01-cached-plans.md); coordinate with [plain resource contracts](resource-mapping-03-plain-contracts.md) before committing to a long-term registration API.

## Goal and context

[`StructurResourceAttribute`](../../src/Synonms.Structur.Core/Attributes/StructurResourceAttribute.cs) currently associates one aggregate root with one resource type and collection path. [`ResourceDirectory`](../../src/Synonms.Structur.Api.Server/Routing/ResourceDirectory.cs) creates one root layout per collection path, and generated controllers and request handlers are closed over that single resource type. This is more than a mapper limitation: resource selection affects routing, authorization, CRUD handlers, versioning, forms, projections, and domain-event/resource generic types.

Add an explicit registration model that associates one aggregate with one or more resource contracts and defines how each is exposed. Use the repeatable resource attribute as the registration declaration; do not retain a separate single-resource registration path.

## Scope and non-goals

In scope: resource registration metadata; validation of mapping pairs and route collisions; read/write capabilities per representation; generic endpoint and handler registration; authorization and route naming; version negotiation; projections/events that currently bind to a resource type; single- and multiple-registration tests.

Out of scope: changing aggregate business rules, automatically deriving secure/public fields, changing persistence records, making all representations writable, replacing the HTTP serialization formats, and deciding a new default versioning protocol without compatibility evidence.

## Proposed design

Represent each exposed contract as a registration containing at least:

- aggregate root type and resource contract type;
- collection path and route/representation identity;
- supported API versions or the applicable selection policy;
- enabled operations (read, create, update, delete, forms) and authorization policy identity;
- optional output/input mapper configuration or custom mapper reference;
- projection association where projection endpoints are exposed.

One aggregate may own multiple registrations. Do not key the directory only by aggregate type or only by collection path. Permit separate paths for distinct audiences/contracts. If multiple registrations share a path, require an explicit, testable discriminator such as negotiated API version or media type; otherwise reject the collision at startup. Never silently pick a registration by enumeration order.

Make `StructurResourceAttribute` repeatable and use it as the single source of truth consumed by route generation, controller discovery, handler registration, OpenAPI/schema generation and mapper selection. Do not add a compatibility lookup or a parallel registration API.

Separate read and write support in the registration. A public projection can be read-only even when an admin representation is writable. If a representation accepts commands, deserialize into that representation, then use validated aggregate factories/commands; do not assume that a successful aggregate-to-resource output binding also defines safe input mapping.

Version selection needs an explicit design decision. Compare:

1. distinct routes/registrations per external version;
2. one route with the existing version context selecting an exact resource contract;
3. one stable resource type whose field-level version metadata filters serialization.

For every option, identify what happens when the requested version is unsupported, when an aggregate has multiple matching resource registrations, and which target representation the output mapper receives. Do not infer a resource version solely from aggregate state or registration order. Preserve current `SupportedVersions`/field-history behavior for existing clients until compatibility tests establish a safe migration.

Audit generic framework surfaces that currently assume one `TAggregateRoot,TResource` pair: `IResourceDirectory` and `ResourceDirectory`; `ControllerFeatureProvider`/`ControllerModelConvention`; endpoint, query, command, and form registrations in `ServiceCollectionExtensions`; route naming/generation; resource response/document factories; OpenAPI; and domain events whose generic base carries a resource type. For events, choose and document either a resource-independent domain-event payload projected by each API registration, or an explicit per-representation event contract. Do not publish a different event merely because a client resource version changes.

## Implementation sequence

1. Map the full registration/request/response/event pipeline and add characterization tests for the current one-resource-per-aggregate behavior.
2. Define the registration model, collision rules, operation/policy metadata and version/event semantics before adding routes.
3. Extend `ResourceDirectory` to expose all validated registrations, with exact pair and explicit-default lookups only.
4. Register mapper pairs, handlers, endpoints, forms, authorization metadata, route names and OpenAPI data from each registration. Ensure read-only registrations do not acquire write endpoints accidentally.
5. Add at least two representations for one test aggregate: distinct collection paths, different public fields and different operation permissions. Add a second version/selection fixture for the chosen version policy.
6. Add a second representation to one sample aggregate and retain single-registration fixtures using the same attribute.
7. Remove obsolete single-resource-only assumptions as part of this rollout.

## Acceptance criteria and targeted regression verification

- One aggregate can map to at least two distinct resource types with independent output fields, route paths, versions and read/write capabilities.
- Single-registration aggregates continue to use the same attribute-based declaration and retain their generated route behavior.
- A registration is selected deterministically for each request and mapping pair. Duplicate route/selector registrations, unknown resource types and ambiguous version matches fail at startup or return the established client error; they never resolve by enumeration order.
- Authorization policies are evaluated for the selected registration, so an admin resource cannot inherit weaker public access accidentally.
- Create/update paths remain validated and only exist for registrations explicitly configured to accept writes.
- Projections, forms, OpenAPI, links and events remain correct for each supported registration; events are not spuriously duplicated per output resource.
- Integration tests cover single- and multi-registration route discovery, request/response mapping, authorization, version negotiation, OpenAPI, and command validation. Run the focused API Server unit/integration suites and build the Sample API.

## Registration and security notes

This is a cross-cutting API registration change. External contracts must be reviewed for sensitive-field exposure; automatic conventions may omit data but must never infer that a field is safe to publish. Require distinct route or version-selection rules and explicit authorization for each representation.

Do not force all registrations to be writable. A versioned output contract can be read-only, and a public projection may intentionally omit fields required for updates. Avoid tying domain event identity and domain behavior to a client resource version; project event data at the API boundary or explicitly document the per-resource event contract.

## Implementation decisions

- `StructurResourceAttribute` is now repeatable and is the registration source consumed by the directory, controller discovery, handlers, mapper resolution, route generation and OpenAPI. There is no separate single-resource lookup or legacy fallback.
- A registration owns a unique collection path made of literal route segments and a unique aggregate/resource pair. Paths are compared case-insensitively; duplicate pairs, paths, and generated method/template collisions fail during `ResourceDirectory` construction. Version/media-type negotiation is not used to disambiguate shared paths.
- A multi-representation aggregate must explicitly identify exactly one default representation; a single-representation aggregate is implicitly its own default. Links to related aggregates use that default registration, while endpoint-generated links use the currently selected registration.
- Representations use distinct collection paths. `SupportedApiVersions` on a registration is an optional exact allow-list for the request version context: requests without a requested version retain existing behavior, while an unsupported explicit version receives the existing applicable-version client fault. Resource property-history filtering and `Resource.GetApplicableVersion` behavior are otherwise unchanged.
- Create, update and delete handlers are closed over the selected aggregate/resource pair. Each writable representation therefore requires its own `IDomainEventFactory<TAggregateRoot,TResource>`. Events remain emitted only for the write request actually selected; changing output representation does not publish extra events.
- Authorization policy names can be overridden per operation on the selected registration; anonymous access and read/create/update/delete capabilities are likewise registration-specific. Create and edit forms can be disabled independently of their corresponding write operation. A read-only contract disables all command and form routes.
- MVC route conventions update the route template/name on each action's existing selector rather than adding a verb-less selector, so the action's HTTP-method constraints remain effective.
