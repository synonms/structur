namespace Synonms.Structur.Application.Schema.Resources;

public enum ResourcePropertyType
{
    Unknown = 0,
    EmbeddedResource,                   // TResource
    EmbeddedChildResource,              // TChildResource
    EmbeddedResourceCollection,         // IEnumerable<TResource>
    EmbeddedChildResourceCollection,    // IEnumerable<TChildResource>
    EmbeddedLookupResource,             // LookupResource
    RelatedResource,                    // EntityId<TAggregateRoot>
    RelatedResourceCollection,          // IEnumerable<EntityId<TAggregateRoot>>
    VanillaCollection,                  // IEnumerable<string> etc.
    VanillaScalar                       // string, int etc.
}