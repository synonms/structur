# Resource mapping 03: plain resource contracts and API envelopes

**Status:** Implemented  
**Scope:** Decouple external resource data from framework-owned transport metadata so consumers can define plain serializable DTOs. Depends on [deterministic cached mapping plans](resource-mapping-01-cached-plans.md); coordinate its registration/endpoint boundaries with [multiple representations](resource-mapping-02-multiple-representations.md).

## Goal and context

Resource classes previously inherited from `Resource` or `ChildResource`. These base types mixed application data with IDs, links, supported-version metadata, and collection-path behavior. Resource discovery and mapping also assumed those base classes, while JSON/Ion converters, envelopes, validation/form generation and schema generation consumed resource types directly.

Separate the external data contract from the API document/envelope that carries framework metadata. A plain contract should be serializable without inheriting framework behavior; the API layer should remain responsible for IDs, links, version metadata and collection/document shape.

## Scope and non-goals

In scope: plain POCO/record contracts; separate root and nested resource data from API envelope metadata; serializer and input binding; validation and forms; OpenAPI/schema generation; version filtering; generated endpoints and typed responses. The alpha framework has no consumer compatibility requirement: remove the former `Resource`/`ChildResource` bases and their adapters.

Out of scope: changing the wire format, removing validation attributes or validation semantics, merging aggregates with resources, making all metadata user-configurable, persistence record changes, and forcing consumers to migrate immediately.

## Proposed design

Introduce a data/envelope boundary. Resource data is a consumer-owned type containing only external properties. A separate API document/envelope holds the data plus framework metadata (identifier, self/related links, applicable representation version and any collection metadata). Child resource data follows the same rule; if nested child IDs/links are part of the protocol, define their envelope separately rather than requiring the data type to inherit `ChildResource`.

Use `ResourceEnvelope<TResource>` and `ResourceMetadata` to carry framework-owned identity and links separately from data. Preserve the characterized default and Ion JSON wire locations for IDs/links, null/default serialization, embedded resources, and collection documents. Resource inheritance is not part of the wire contract.

Keep API metadata adjacent to the API contract, but separate its ownership from plain data. Validation/schema/form attributes that describe the public input contract may remain on plain DTO members if the frameworks can inspect them without a `Resource` base. Document attribute behavior on records/init-only members and nested values. Metadata used only for routing or transport belongs on the registration/envelope, not on the plain DTO.

Mapping output should target the data contract, then the API assembly layer should attach IDs and links. Input should deserialize the contract, run the same validation and aggregate command/factory path as today, and not accept or trust framework links as domain input. Ensure adding a same-name data property such as `Id` or `Links` cannot override envelope-owned values unless explicitly allowed.

Versioning requires a clear owner. Prefer registration/envelope metadata to describe supported resource versions, while property history remains an explicit schema/input/output concern. Preserve current version resolution and field filtering until tests demonstrate equivalent behavior for plain contracts. A plain contract should not need to inherit a mutable `SortedSet<Version>` to participate.

## Implementation sequence

1. Characterize current wire output and input for root/child resources in default and Ion formats: IDs, links, fields, nulls, embedded resources, collections, versions, forms and OpenAPI schemas.
2. Define the document/envelope/data interfaces or adapters, with separate root and child behavior. Prove the design using one read-only plain DTO before changing endpoint generics.
3. Add serializer/converter support that serializes the data contract in the same wire location and shape as existing resources while adding framework metadata from the envelope. Keep old converters and resource base types functional.
4. Update cached output plans and request binding to map data contracts, preserve validated command construction, and reject attempts to bind envelope metadata as domain values.
5. Update form generation, validation, schema/OpenAPI factories, version filtering, embedded-resource handling and collection responses to consume the new contract abstraction.
6. Add end-to-end plain root and nested contract fixtures, including a record contract, and migrate the Sample API to plain data plus envelope metadata.
7. Document the breaking migration and the envelope-only registration, mapping, client, and event factory surfaces.

## Acceptance criteria and targeted regression verification

- A consumer can define a root external data type and nested data type without inheriting `Resource` or `ChildResource`.
- Framework-owned ID/link/version data is carried by the API document/envelope and cannot silently be overwritten by DTO members.
- Root and nested plain contracts serialize, deserialize, generate forms/schemas and map without framework inheritance.
- The default and Ion wire representations remain byte/shape compatible for characterized cases, including link relations, IDs, null handling and embedded collections.
- Plain DTO input still receives the same validation faults and aggregate-level validation; DTO deserialization never creates or rehydrates domain objects directly.
- Version negotiation and field filtering follow registration-owned API versions and explicit property history.
- API Server unit and integration tests cover plain root/nested contracts; serializer, form, OpenAPI, routing and mapping tests pass. Build the API Core, API Server and Sample API projects.

## Compatibility, rollout and pitfalls

This is a deliberate alpha breaking change: there is no legacy resource adapter or parallel base-class path. Wire shape—not CLR inheritance—is the compatibility contract. Do not move version, links, or ID data into a DTO merely to simplify mapping, and do not assume serializer behavior on plain records matches the former custom converters.

Some existing sample resources use API validation attributes and initializers directly. Verify validation discovery for POCO/record properties, nullable references and nested contracts before migrating them. Keep request validation separate from output projection and from persistence rehydration.

## Implementation progress

- Added `ResourceEnvelope<TResource>` / `ResourceMetadata`; root and nested mapping carries IDs and links separately from DTO data. Embedded metadata survives JSON input parsing and event persistence/replay; default and Ion writers retain the characterized wire locations.
- Removed `Resource`/`ChildResource`, their dedicated JSON converters, inheritance constraints, data-only command/event-factory overloads, and reflection-based ID access. Mapping, endpoints, commands, documents, clients, forms, schema generation, testing helpers, and Sample API contracts now use plain root/nested data types.
- Request and client document readers reserve metadata names so DTO properties cannot receive or override envelope-owned values. Registration metadata owns supported API versions; property-history attributes remain the field filtering mechanism.
- Forms/schema generation, POCO and positional record mapping, default/Ion serialization, nested IDs, routing, and multi-representation paths have regression coverage.
