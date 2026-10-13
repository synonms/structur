namespace Synonms.Structur.Api.Core.Schema.Resources;

public enum ResourcePropertyType
{
    Unknown = 0,
    EmbeddedResource,
    EmbeddedResourceCollection,
    /// <summary>
    /// LookupResource
    /// </summary>
    EmbeddedLookupResource,
    /// <summary>
    /// EntityId&lt;TAggregateRoot&gt;
    /// </summary>
    RelatedResource,
    /// <summary>
    /// IEnumerable&lt;EntityId&lt;TAggregateRoot&gt;&gt;
    /// </summary>
    RelatedResourceCollection,
    /// <summary>
    /// Enum
    /// </summary>
    Enumeration,
    /// <summary>
    /// IEnumerable&lt;string&gt; etc.
    /// </summary>
    VanillaCollection,
    /// <summary>
    /// string, int etc.
    /// </summary>
    VanillaScalar 
}