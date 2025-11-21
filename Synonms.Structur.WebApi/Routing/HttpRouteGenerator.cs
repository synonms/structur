using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Synonms.Structur.Application.Pipeline;
using Synonms.Structur.Application.Routing;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Routing;

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
        
        return new Uri(uriString + queryString);
    }

    public Uri Item(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();
        
        string routeName = _routeNameProvider.GetById(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }

    public Uri Collection<TAggregateRoot>(QueryParameters? queryParameters = null) 
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.GetAll<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }

    public Uri Collection(Type aggregateRootType, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.GetAll(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }
    
    public Uri CreateForm<TAggregateRoot>(QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.CreateForm<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }

    public Uri CreateForm(Type aggregateRootType, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.CreateForm(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, null, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }

    public Uri EditForm<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.EditForm<TAggregateRoot>();
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }

    public Uri EditForm(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null)
    {
        HttpContext httpContext = _httpContextAccessor.HttpContext ?? new DefaultHttpContext();

        string routeName = _routeNameProvider.EditForm(aggregateRootType);
        string uriString = _linkGenerator.GetUriByRouteValues(httpContext, routeName, new { id }, options: RoutingConfiguration.DefaultLinkOptions) ?? string.Empty;
        string queryString = queryParameters?.Any() ?? false ? queryParameters.ToQueryString() : string.Empty;
        
        return new Uri(uriString + queryString);
    }
}