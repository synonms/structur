using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Versioning.Resolution;

public class QueryStringVersionResolutionStrategy : IVersionResolutionStrategy
{
    private static readonly Func<KeyValuePair<string, StringValues>, bool> QueryPredicate =
        query => query.Key.Equals(HttpQueryStringKeys.ApiVersion, StringComparison.OrdinalIgnoreCase);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryStringVersionResolutionStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
        
    public Maybe<Version> Resolve()
    {
        if (_httpContextAccessor?.HttpContext is null)
        {
            return Maybe<Version>.None;
        }
            
        if (_httpContextAccessor.HttpContext.Request.Query.Count(QueryPredicate) != 1)
        {
            return Maybe<Version>.None;
        }

        KeyValuePair<string, StringValues> versionQuery = _httpContextAccessor.HttpContext.Request.Query.Single(QueryPredicate);

        if (versionQuery.Value.Count != 1)
        {
            return Maybe<Version>.None;
        }

        string? versionAsString = versionQuery.Value.SingleOrDefault();

        return Version.TryParse(versionAsString, out Version? version) ? version : Maybe<Version>.None;
    }
}