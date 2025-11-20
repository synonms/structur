using System.Reflection;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.System.Reflection;

namespace Synonms.Structur.WebApi.Routing;

public class ResourceDirectory : IResourceDirectory
{
    private static readonly Dictionary<string, IResourceDirectory.AggregateRootLayout> ResourceCollectionPathToAggregateRootLayout = new();
    private static readonly List<IResourceDirectory.AggregateMemberLayout> AggregateMemberLayouts = new();
    
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
        
        foreach (Type aggregateRootType in assemblies.SelectMany(assembly => assembly.GetAggregateRoots()))
        {
            StructurResourceAttribute? attribute = aggregateRootType.GetCustomAttribute<StructurResourceAttribute>();

            if (attribute is not null)
            {
                ResourceCollectionPathToAggregateRootLayout[attribute.CollectionPath] = new IResourceDirectory.AggregateRootLayout(aggregateRootType, attribute.ResourceType);
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
}