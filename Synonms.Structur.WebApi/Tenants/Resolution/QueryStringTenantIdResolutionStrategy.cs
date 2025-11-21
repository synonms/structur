using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Application.Tenants.Resolution;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Http;

namespace Synonms.Structur.WebApi.Tenants.Resolution;

public class QueryStringTenantIdResolutionStrategy : ITenantIdResolutionStrategy
{
    private static readonly Func<KeyValuePair<string, StringValues>, bool> QueryPredicate =
        query => query.Key.Equals(HttpQueryStringKeys.TenantId, StringComparison.OrdinalIgnoreCase);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryStringTenantIdResolutionStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
        
    public Maybe<Guid> Resolve()
    {
        if (_httpContextAccessor?.HttpContext is null)
        {
            return Maybe<Guid>.None;
        }
            
        if (CountApplicableQueries(_httpContextAccessor.HttpContext.Request) != 1)
        {
            return Maybe<Guid>.None;
        }

        KeyValuePair<string, StringValues> tenantIdQuery = _httpContextAccessor.HttpContext.Request.Query.Single(QueryPredicate);

        if (tenantIdQuery.Value.Count != 1)
        {
            return Maybe<Guid>.None;
        }

        string? tenantId = tenantIdQuery.Value.SingleOrDefault();

        return Guid.TryParse(tenantId, out Guid guid) ? guid : Maybe<Guid>.None;
    }
        
    private static int CountApplicableQueries(HttpRequest httpRequest) =>
        httpRequest.Query.Count(QueryPredicate);
}