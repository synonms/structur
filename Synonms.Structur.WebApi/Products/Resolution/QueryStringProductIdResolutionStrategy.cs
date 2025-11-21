using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Application.Products.Resolution;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Http;

namespace Synonms.Structur.WebApi.Products.Resolution;

public class QueryStringProductIdResolutionStrategy : IProductIdResolutionStrategy
{
    private static readonly Func<KeyValuePair<string, StringValues>, bool> QueryPredicate =
        query => query.Key.Equals(HttpQueryStringKeys.ProductId, StringComparison.OrdinalIgnoreCase);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public QueryStringProductIdResolutionStrategy(IHttpContextAccessor httpContextAccessor)
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

        KeyValuePair<string, StringValues> productIdQuery = _httpContextAccessor.HttpContext.Request.Query.Single(QueryPredicate);

        if (productIdQuery.Value.Count != 1)
        {
            return Maybe<Guid>.None;
        }

        string? productId = productIdQuery.Value.SingleOrDefault();

        return Guid.TryParse(productId, out Guid guid) ? guid : Maybe<Guid>.None;
    }
        
    private static int CountApplicableQueries(HttpRequest httpRequest) =>
        httpRequest.Query.Count(QueryPredicate);
}