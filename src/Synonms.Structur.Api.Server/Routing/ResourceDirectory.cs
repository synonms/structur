using System.Reflection;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.System.Reflection;

namespace Synonms.Structur.Api.Server.Routing;

public class ResourceDirectory : IResourceDirectory
{
    private static readonly Dictionary<string, IResourceDirectory.AggregateRootLayout> ResourceCollectionPathToAggregateRootLayout = new();
    private static readonly List<IResourceDirectory.AggregateMemberLayout> AggregateMemberLayouts = [];
    
    public ResourceDirectory(params Assembly[] assemblies)
    {
        Construct(assemblies);
    }
    
    public IReadOnlyDictionary<string, IResourceDirectory.AggregateRootLayout> GetAllRoots() =>
        ResourceCollectionPathToAggregateRootLayout;

    public IEnumerable<IResourceDirectory.AggregateMemberLayout> GetAllMembers() =>
        AggregateMemberLayouts;
    
    private static void Construct(params Assembly[] assemblies)
    {
        ResourceCollectionPathToAggregateRootLayout.Clear();
        AggregateMemberLayouts.Clear();
        
        Dictionary<Type, List<Type>> projectionTypesByAggregateRootType = GetProjectionTypesByAggregateRootType(assemblies);
        
        foreach (Type aggregateRootType in assemblies.SelectMany(assembly => assembly.GetAggregateRoots()))
        {
            StructurResourceAttribute? attribute = aggregateRootType.GetCustomAttribute<StructurResourceAttribute>();

            if (attribute is not null)
            {
                List<Type> projectionTypesForAggregateRoot = projectionTypesByAggregateRootType.TryGetValue(aggregateRootType, out List<Type>? projectionTypes) ? projectionTypes : [];
                
                ResourceCollectionPathToAggregateRootLayout[attribute.CollectionPath] = new IResourceDirectory.AggregateRootLayout(aggregateRootType, attribute.ResourceType, projectionTypesForAggregateRoot);
            }
        }

        foreach (Type aggregateMemberType in assemblies.SelectMany(assembly => assembly.GetAggregateMembers()))
        {
            StructurChildResourceAttribute? attribute = aggregateMemberType.GetCustomAttribute<StructurChildResourceAttribute>();

            if (attribute is not null)
            {
                AggregateMemberLayouts.Add(new IResourceDirectory.AggregateMemberLayout(aggregateMemberType, attribute.ChildResourceType));
            }
        }
    }

    private static Dictionary<Type, List<Type>> GetProjectionTypesByAggregateRootType(Assembly[] assemblies)
    {
        Dictionary<Type, List<Type>> projectionTypesByAggregateRootType = new();
        
        foreach (Type projectionType in assemblies.SelectMany(assembly => assembly.GetProjections()))
        {
            StructurProjectionAttribute? attribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();

            if (attribute is not null)
            {
                if (!projectionTypesByAggregateRootType.ContainsKey(attribute.AggregateRootType))
                {
                    projectionTypesByAggregateRootType.Add(attribute.AggregateRootType, []);
                }
                
                projectionTypesByAggregateRootType[attribute.AggregateRootType].Add(projectionType);
            }
        }

        return projectionTypesByAggregateRootType;
    }
}