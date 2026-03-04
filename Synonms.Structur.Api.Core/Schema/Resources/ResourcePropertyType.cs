namespace Synonms.Structur.Api.Core.Schema.Resources;

public enum ResourcePropertyType
{
    Unknown = 0,
    /// <summary>
    /// TResource
    /// </summary>
    EmbeddedResource,
    /// <summary>
    /// TChildResource
    /// </summary>
    EmbeddedChildResource,
    /// <summary>
    /// IEnumerable&lt;Resource&gt;
    /// </summary>
    EmbeddedResourceCollection,
    /// <summary>
    /// IEnumerable&lt;TChildResource&gt;
    /// </summary>
    EmbeddedChildResourceCollection,
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
    /// ValueObjectResource
    /// </summary>
    ComplexValueObjectResource,
    /// <summary>
    /// IEnumerable&lt;ValueObjectResource&gt;
    /// </summary>
    ComplexValueObjectResourceCollection,
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