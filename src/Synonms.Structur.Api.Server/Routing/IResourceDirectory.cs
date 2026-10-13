using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Server.Routing;

public interface IResourceDirectory
{
    public class AggregateRootLayout(Type aggregateRootType, Type resourceType, string collectionPath, StructurResourceAttribute registration, bool isDefaultRepresentation, IReadOnlySet<Version> supportedApiVersions, List<Type> projectionTypes)
    {
        public Type AggregateRootType { get; } = aggregateRootType;

        public Type ResourceType { get; } = resourceType;

        public string CollectionPath { get; } = collectionPath;

        public StructurResourceAttribute Registration { get; } = registration;

        public bool IsDefaultRepresentation { get; } = isDefaultRepresentation;

        public IReadOnlySet<Version> SupportedApiVersions { get; } = supportedApiVersions;

        public IReadOnlyList<Type> ProjectionTypes { get; } = projectionTypes;
    }

    public class AggregateMemberLayout(Type aggregateMemberType, Type childResourceType)
    {
        public Type AggregateMemberType { get; } = aggregateMemberType;

        public Type ChildResourceType { get; } = childResourceType;
    }

    IReadOnlyCollection<AggregateRootLayout> GetAllRoots();

    AggregateRootLayout GetRoot(Type aggregateRootType, Type resourceType);

    AggregateRootLayout GetDefaultRoot(Type aggregateRootType);

    IEnumerable<AggregateMemberLayout> GetAllMembers();
}
