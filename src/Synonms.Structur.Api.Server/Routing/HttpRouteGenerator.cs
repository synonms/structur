using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Api.Server.Routing;

/// <summary>
/// All URIs changed to Relative to skirt issue with Docker containers where Absolute URIs report the internal protocol/port rather than the external host.
/// </summary>
public class HttpRouteGenerator : IRouteGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IRouteNameProvider _routeNameProvider;
    private readonly IResourceDirectory _resourceDirectory;

    public HttpRouteGenerator(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, IRouteNameProvider routeNameProvider, IResourceDirectory resourceDirectory)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _routeNameProvider = routeNameProvider;
        _resourceDirectory = resourceDirectory;
    }

    public Uri Item<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        return Item(typeof(TAggregateRoot), id.Value, queryParameters);
    }

    public Uri Item(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null)
    {
        IResourceDirectory.AggregateRootLayout layout = _resourceDirectory.GetDefaultRoot(aggregateRootType);
        return Item(aggregateRootType, layout.ResourceType, id, queryParameters);
    }

    public Uri Item(Type aggregateRootType, Type resourceType, Guid id, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        string routeName = _routeNameProvider.GetById(aggregateRootType, resourceType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri Collection<TAggregateRoot>(QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        return Collection(typeof(TAggregateRoot), queryParameters);
    }

    public Uri Collection(Type aggregateRootType, QueryParameters? queryParameters = null)
    {
        IResourceDirectory.AggregateRootLayout layout = _resourceDirectory.GetDefaultRoot(aggregateRootType);
        return Collection(aggregateRootType, layout.ResourceType, queryParameters);
    }

    public Uri Collection(Type aggregateRootType, Type resourceType, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        string routeName = _routeNameProvider.GetAll(aggregateRootType, resourceType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri CreateForm<TAggregateRoot>(QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        return CreateForm(typeof(TAggregateRoot), queryParameters);
    }

    public Uri CreateForm(Type aggregateRootType, QueryParameters? queryParameters = null)
    {
        IResourceDirectory.AggregateRootLayout layout = _resourceDirectory.GetDefaultRoot(aggregateRootType);
        return CreateForm(aggregateRootType, layout.ResourceType, queryParameters);
    }

    public Uri CreateForm(Type aggregateRootType, Type resourceType, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        string routeName = _routeNameProvider.CreateForm(aggregateRootType, resourceType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri EditForm<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        return EditForm(typeof(TAggregateRoot), id.Value, queryParameters);
    }

    public Uri EditForm(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null)
    {
        IResourceDirectory.AggregateRootLayout layout = _resourceDirectory.GetDefaultRoot(aggregateRootType);
        return EditForm(aggregateRootType, layout.ResourceType, id, queryParameters);
    }

    public Uri EditForm(Type aggregateRootType, Type resourceType, Guid id, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        string routeName = _routeNameProvider.EditForm(aggregateRootType, resourceType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri Projection<TAggregateRoot, TProjection>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
        where TProjection : Projection<TAggregateRoot> =>
        Projection(typeof(TAggregateRoot), typeof(TProjection), id.Value, queryParameters);

    public Uri Projection(Type aggregateRootType, Type projectionType, Guid id, QueryParameters? queryParameters = null)
    {
        StructurProjectionAttribute? projectionAttribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();

        if (projectionAttribute is null)
        {
            return new Uri("/", UriKind.Relative);
        }

        IResourceDirectory.AggregateRootLayout layout = _resourceDirectory.GetDefaultRoot(aggregateRootType);
        return Projection(aggregateRootType, layout.ResourceType, projectionType, id, queryParameters);
    }

    public Uri Projection(Type aggregateRootType, Type resourceType, Type projectionType, Guid id, QueryParameters? queryParameters = null)
    {
        StructurProjectionAttribute? projectionAttribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();
        if (projectionAttribute is null)
        {
            return new Uri("/", UriKind.Relative);
        }

        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        string routeName = _routeNameProvider.GetById(aggregateRootType, resourceType);
        string itemUri = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        return new Uri(itemUri + "/projections/" + projectionAttribute.ProjectionIdentifier + queryString).ToRelativeUri();
    }
}
