using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Routing;

/// <summary>
/// All URIs changed to Relative to skirt issue with Docker containers where Absolute URIs report the internal protocol/port rather than the external host. 
/// </summary>
public class HttpRouteGenerator : IRouteGenerator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly LinkGenerator _linkGenerator;
    private readonly IRouteNameProvider _routeNameProvider;

    public HttpRouteGenerator(IHttpContextAccessor httpContextAccessor, LinkGenerator linkGenerator, IRouteNameProvider routeNameProvider)
    {
        _httpContextAccessor = httpContextAccessor;
        _linkGenerator = linkGenerator;
        _routeNameProvider = routeNameProvider;
    }

    public Uri Item<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null) 
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        
        string routeName = _routeNameProvider.GetById<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id = id.Value }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri Item(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        
        string routeName = _routeNameProvider.GetById(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri Collection<TAggregateRoot>(QueryParameters? queryParameters = null) 
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.GetAll<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri Collection(Type aggregateRootType, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.GetAll(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }
    
    public Uri CreateForm<TAggregateRoot>(QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.CreateForm<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri CreateForm(Type aggregateRootType, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.CreateForm(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri EditForm<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.EditForm<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }

    public Uri EditForm(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.EditForm(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString).ToRelativeUri();
    }
}