namespace Synonms.Structur.Api.Server.Routing;

public interface IResourceDirectory
{
    public class AggregateRootLayout(Type aggregateRootType, Type resourceType, List<Type> projectionTypes)
    {
        public Type AggregateRootType { get; set; } = aggregateRootType;

        public Type ResourceType { get; set; } = resourceType;
        
        public List<Type> ProjectionTypes { get; set; } = projectionTypes;
    }

    public class AggregateMemberLayout(Type aggregateMemberType, Type childResourceType)
    {
        public Type AggregateMemberType { get; set; } = aggregateMemberType;

        public Type ChildResourceType { get; set; } = childResourceType;
    }

    IReadOnlyDictionary<string, AggregateRootLayout> GetAllRoots();
    
    IEnumerable<AggregateMemberLayout> GetAllMembers();
}