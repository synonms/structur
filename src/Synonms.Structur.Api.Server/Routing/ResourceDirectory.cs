using System.Reflection;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.System.Reflection;

namespace Synonms.Structur.Api.Server.Routing;

public class ResourceDirectory : IResourceDirectory
{
    private readonly IReadOnlyList<IResourceDirectory.AggregateRootLayout> _aggregateRootLayouts;
    private readonly IReadOnlyList<IResourceDirectory.AggregateMemberLayout> _aggregateMemberLayouts;

    public ResourceDirectory(params Assembly[] assemblies)
    {
        (_aggregateRootLayouts, _aggregateMemberLayouts) = Construct(assemblies);
    }

    public IReadOnlyCollection<IResourceDirectory.AggregateRootLayout> GetAllRoots() =>
        _aggregateRootLayouts;

    public IEnumerable<IResourceDirectory.AggregateMemberLayout> GetAllMembers() =>
        _aggregateMemberLayouts;

    public IResourceDirectory.AggregateRootLayout GetRoot(Type aggregateRootType, Type resourceType) =>
        _aggregateRootLayouts.SingleOrDefault(layout =>
            layout.AggregateRootType == aggregateRootType && layout.ResourceType == resourceType)
        ?? throw new InvalidOperationException($"No resource registration exists for {aggregateRootType.FullName} -> {resourceType.FullName}.");

    public IResourceDirectory.AggregateRootLayout GetDefaultRoot(Type aggregateRootType) =>
        _aggregateRootLayouts.SingleOrDefault(layout => layout.AggregateRootType == aggregateRootType && layout.IsDefaultRepresentation)
        ?? throw new InvalidOperationException($"Aggregate {aggregateRootType.FullName} does not have exactly one default resource representation.");

    private static (IReadOnlyList<IResourceDirectory.AggregateRootLayout> Roots, IReadOnlyList<IResourceDirectory.AggregateMemberLayout> Members) Construct(params Assembly[] assemblies)
    {
        assemblies = assemblies.Distinct().ToArray();
        Dictionary<Type, List<Type>> projectionTypesByAggregateRootType = GetProjectionTypesByAggregateRootType(assemblies);
        List<IResourceDirectory.AggregateRootLayout> aggregateRootLayouts = [];

        foreach (Type aggregateRootType in assemblies.SelectMany(assembly => assembly.GetAggregateRoots()))
        {
            StructurResourceAttribute[] registrations = aggregateRootType.GetCustomAttributes<StructurResourceAttribute>().ToArray();
            foreach (StructurResourceAttribute attribute in registrations)
            {
                if (string.IsNullOrWhiteSpace(attribute.CollectionPath))
                {
                    throw new InvalidOperationException($"Resource collection path must be specified for {aggregateRootType.FullName} -> {attribute.ResourceType.FullName}.");
                }

                if (!attribute.ResourceType.IsClass
                    || attribute.ResourceType.IsAbstract
                    || attribute.ResourceType.ContainsGenericParameters
                    || attribute.ResourceType == typeof(string))
                {
                    throw new InvalidOperationException($"Resource registration for {aggregateRootType.FullName} must target a concrete plain resource contract type; found {attribute.ResourceType.FullName}.");
                }

                string collectionPath = attribute.CollectionPath.Trim('/');
                if (string.IsNullOrWhiteSpace(collectionPath))
                {
                    throw new InvalidOperationException($"Resource collection path must contain a route segment for {aggregateRootType.FullName} -> {attribute.ResourceType.FullName}.");
                }

                if (collectionPath.Split('/').Any(segment => segment.Contains('{') || segment.Contains('}')))
                {
                    throw new InvalidOperationException($"Resource collection path '{collectionPath}' for {aggregateRootType.FullName} -> {attribute.ResourceType.FullName} must contain literal route segments only.");
                }

                HashSet<Version> supportedVersions = [];
                foreach (string versionText in attribute.SupportedApiVersions ?? [])
                {
                    if (!Version.TryParse(versionText, out Version? version))
                    {
                        throw new InvalidOperationException($"Resource registration for {aggregateRootType.FullName} -> {attribute.ResourceType.FullName} contains invalid API version '{versionText}'. Use a numeric version such as '1.0'.");
                    }

                    if (!supportedVersions.Add(version))
                    {
                        throw new InvalidOperationException($"Resource registration for {aggregateRootType.FullName} -> {attribute.ResourceType.FullName} repeats supported API version '{version}'.");
                    }
                }

                List<Type> projectionTypesForAggregateRoot = projectionTypesByAggregateRootType.TryGetValue(aggregateRootType, out List<Type>? projectionTypes) ? projectionTypes : [];
                aggregateRootLayouts.Add(new IResourceDirectory.AggregateRootLayout(
                    aggregateRootType,
                    attribute.ResourceType,
                    collectionPath,
                    attribute,
                    registrations.Length == 1 || attribute.IsDefaultRepresentation,
                    supportedVersions,
                    projectionTypesForAggregateRoot));
            }
        }

        ValidateRootRegistrations(aggregateRootLayouts);

        List<IResourceDirectory.AggregateMemberLayout> aggregateMemberLayouts = [];

        foreach (Type aggregateMemberType in assemblies.SelectMany(assembly => assembly.GetAggregateMembers()))
        {
            StructurChildResourceAttribute? attribute = aggregateMemberType.GetCustomAttribute<StructurChildResourceAttribute>();

            if (attribute is not null)
            {
                aggregateMemberLayouts.Add(new IResourceDirectory.AggregateMemberLayout(aggregateMemberType, attribute.ChildResourceType));
            }
        }

        return (aggregateRootLayouts, aggregateMemberLayouts);
    }

    internal static void ValidateRootRegistrations(IReadOnlyCollection<IResourceDirectory.AggregateRootLayout> registrations)
    {
        IResourceDirectory.AggregateRootLayout? duplicatePair = registrations
            .GroupBy(layout => (layout.AggregateRootType, layout.ResourceType))
            .Where(group => group.Count() > 1)
            .Select(group => group.First())
            .FirstOrDefault();

        if (duplicatePair is not null)
        {
            throw new InvalidOperationException($"Duplicate resource registration for {duplicatePair.AggregateRootType.FullName} -> {duplicatePair.ResourceType.FullName}. Each aggregate/resource pair must have exactly one registration.");
        }

        IResourceDirectory.AggregateRootLayout? duplicatePath = registrations
            .GroupBy(layout => layout.CollectionPath, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.First())
            .FirstOrDefault();

        if (duplicatePath is not null)
        {
            throw new InvalidOperationException($"Resource collection path '{duplicatePath.CollectionPath}' is registered more than once. Representations must use distinct paths; selection by enumeration order is not supported.");
        }

        foreach (IGrouping<Type, IResourceDirectory.AggregateRootLayout> aggregateRegistrations in registrations.GroupBy(layout => layout.AggregateRootType))
        {
            IResourceDirectory.AggregateRootLayout[] aggregateLayouts = aggregateRegistrations.ToArray();
            int defaultCount = aggregateLayouts.Count(layout => layout.IsDefaultRepresentation);
            if (defaultCount != 1)
            {
                throw new InvalidOperationException(
                    $"Aggregate {aggregateRegistrations.Key.FullName} has {aggregateLayouts.Length} resource representations and must declare exactly one as the default representation.");
            }
        }

        ValidateGeneratedRoutes(registrations);
    }

    private static void ValidateGeneratedRoutes(IEnumerable<IResourceDirectory.AggregateRootLayout> registrations)
    {
        Dictionary<(string Method, string Template), IResourceDirectory.AggregateRootLayout> routeOwners = [];

        foreach (IResourceDirectory.AggregateRootLayout layout in registrations)
        {
            StructurResourceAttribute registration = layout.Registration;
            string collectionPath = layout.CollectionPath;

            if (!registration.IsReadDisabled)
            {
                Add("GET", collectionPath, layout);
                Add("GET", collectionPath + "/{id}", layout);

                foreach (Type projectionType in layout.ProjectionTypes)
                {
                    StructurProjectionAttribute? projection = projectionType.GetCustomAttribute<StructurProjectionAttribute>();
                    if (projection is not null)
                    {
                        Add("GET", collectionPath + "/{id}/projections/" + projection.ProjectionIdentifier, layout);
                    }
                }
            }

            if (!registration.IsCreateDisabled)
            {
                Add("POST", collectionPath, layout);
                if (!registration.IsCreateFormDisabled)
                {
                    Add("GET", collectionPath + "/create-form", layout);
                }
            }

            if (!registration.IsUpdateDisabled)
            {
                Add("PUT", collectionPath + "/{id}", layout);
                if (!registration.IsEditFormDisabled)
                {
                    Add("GET", collectionPath + "/{id}/edit-form", layout);
                }
            }

            if (!registration.IsDeleteDisabled)
            {
                Add("DELETE", collectionPath + "/{id}", layout);
            }
        }

        void Add(string method, string template, IResourceDirectory.AggregateRootLayout layout)
        {
            string normalizedTemplate = string.Join('/',
                template.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Select(segment => segment.StartsWith('{') && segment.EndsWith('}')
                        ? "{}"
                        : segment.ToLowerInvariant()));
            (string Method, string Template) key = (method, normalizedTemplate);
            if (routeOwners.TryGetValue(key, out IResourceDirectory.AggregateRootLayout? existing))
            {
                throw new InvalidOperationException(
                    $"Generated {method} route '/{template}' for {layout.AggregateRootType.FullName} -> {layout.ResourceType.FullName} collides with the route for {existing.AggregateRootType.FullName} -> {existing.ResourceType.FullName}. Resource registrations must have unambiguous generated routes.");
            }

            routeOwners.Add(key, layout);
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
