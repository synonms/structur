# Plain resource contracts

Resource data is consumer-owned. A root or nested data contract can be a POCO or record and does not need to inherit a framework base class. Public contract properties remain the source for validation/form hints, schema generation, version-history filtering, and mapping conventions.

Framework metadata is represented separately by `ResourceEnvelope<TResource>` and `ResourceMetadata`. The envelope carries the data, API identifier, self link, and links. Root and child mappers expose `MapEnvelope` when the API representation is needed; `Map` returns only the data contract. A parent envelope retains the metadata for mapped embedded objects and collections so IDs and links are emitted in the existing wire locations without adding framework state to DTOs.

`ResourceDocument<TResource>` and `ResourceCollectionDocument<TResource>` are assembled from envelopes. Do not construct a resource document directly from a DTO and a link: supply the envelope so the serializer has authoritative metadata. The default representation keeps each item identifier in the value object and document links outside it. Ion JSON keeps item self/related links in each value object and the document links at the document level. Property order/null behavior follows the configured serializer and version-history rules.

`Id`, `Links`, `SelfLink`, and `SupportedVersions` are reserved framework metadata names. They are excluded from resource mapping, generated forms/OpenAPI data properties, envelope-based output, and request deserialization even if a DTO declares a same-named member. Request readers extract root and nested IDs into envelope metadata, ignore link-shaped input, and leave DTO members at their defaults; update routes supply the authoritative root ID. Consumers should not model links or supported API versions as data members.

API versions supported by a representation belong on its `StructurResourceAttribute` registration. Property history remains on public contract members and controls output-field filtering. Plain record contracts can use public constructors whose arguments correspond to public properties; writable/init-only members are populated by the mapping plan.

The former `Resource` and `ChildResource` base classes and their dedicated JSON converters have been removed. Domain event factories receive `ResourceEnvelope<TResource>` so root and nested identity remains separate from consumer data and survives event replay. This is an intentional breaking change; registrations, clients, and request handlers use plain contracts only.
