using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Application.Tenants.Resolution;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Http;

namespace Synonms.Structur.WebApi.Tenants.Resolution;

public class HeaderTenantIdResolutionStrategy : ITenantIdResolutionStrategy
{
    private static readonly Func<KeyValuePair<string, StringValues>, bool> HeaderPredicate =
        header => header.Key.Equals(HttpHeaders.TenantId, StringComparison.OrdinalIgnoreCase);

    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderTenantIdResolutionStrategy(IHttpContextAccessor httpContextAccessor)
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

        KeyValuePair<string, StringValues> tenantIdHeader = _httpContextAccessor.HttpContext.Request.Headers.Single(HeaderPredicate);

        if (tenantIdHeader.Value.Count != 1)
        {
            return Maybe<Guid>.None;
        }

        string? tenantId = tenantIdHeader.Value.SingleOrDefault();

        return Guid.TryParse(tenantId, out Guid guid) ? guid : Maybe<Guid>.None;
    }

    private static int CountApplicableHeaders(HttpRequest httpRequest) =>
        httpRequest.Headers.Count(HeaderPredicate);
}