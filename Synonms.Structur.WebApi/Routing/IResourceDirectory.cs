namespace Synonms.Structur.WebApi.Routing;

public interface IResourceDirectory
{
    public class AggregateRootLayout(Type aggregateRootType, Type resourceType)
    {
        public Type AggregateRootType { get; set; } = aggregateRootType;

        public Type ResourceType { get; set; } = resourceType;
    }

    public class AggregateMemberLayout(Type aggregateMemberType, Type childResourceType)
    {
        public Type AggregateMemberType { get; set; } = aggregateMemberType;

        public Type ChildResourceType { get; set; } = childResourceType;
    }

    IReadOnlyDictionary<string, AggregateRootLayout> GetAllRoots();
    
    IEnumerable<AggregateMemberLayout> GetAllMembers();
}