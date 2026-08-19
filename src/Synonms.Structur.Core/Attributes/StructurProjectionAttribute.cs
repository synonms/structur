namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StructurProjectionAttribute : Attribute
{
    public StructurProjectionAttribute(Type aggregateRootType, string projectionIdentifier, string name, string? description = null, bool allowAnonymous = false)
    {
        AggregateRootType = aggregateRootType;
        ProjectionIdentifier = projectionIdentifier;
        Name = name;
        Description = description;
        AllowAnonymous = allowAnonymous;
    }

    public Type AggregateRootType { get; }
    
    public string ProjectionIdentifier { get; }
    
    public string Name { get; }
    
    public string? Description { get; }

    public bool AllowAnonymous { get; }
}