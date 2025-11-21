using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Application.Products.Resolution;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Http;

namespace Synonms.Structur.WebApi.Products.Resolution;

public class HeaderProductIdResolutionStrategy : IProductIdResolutionStrategy
{
    private static readonly Func<KeyValuePair<string, StringValues>, bool> HeaderPredicate =
        header => header.Key.Equals(HttpHeaders.ProductId, StringComparison.OrdinalIgnoreCase);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderProductIdResolutionStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Maybe<Guid> Resolve()
    {
        if (_httpContextAccessor?.HttpContext is null)
        {
            return Maybe<Guid>.None;
        }

        if (CountApplicableHeaders(_httpContextAccessor.HttpContext.Request) != 1)
        {
            return Maybe<Guid>.None;
        }

        KeyValuePair<string, StringValues> productIdHeader = _httpContextAccessor.HttpContext.Request.Headers.Single(HeaderPredicate);

        if (productIdHeader.Value.Count != 1)
        {
            return Maybe<Guid>.None;
        }

        string? productId = productIdHeader.Value.SingleOrDefault();

        return Guid.TryParse(productId, out Guid guid) ? guid : Maybe<Guid>.None;
    }

    private static int CountApplicableHeaders(HttpRequest httpRequest) =>
        httpRequest.Headers.Count(HeaderPredicate);
}