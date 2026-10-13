using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Versioning.Resolution;

public class HeaderVersionResolutionStrategy : IVersionResolutionStrategy
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HeaderVersionResolutionStrategy(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Maybe<Version> Resolve()
    {
        if (_httpContextAccessor?.HttpContext is null)
        {
            return Maybe<Version>.None;
        }

        IHeaderDictionary headers = _httpContextAccessor.HttpContext.Request.Headers;
        if (headers.TryGetValue(HttpHeaders.ApiVersion, out StringValues apiVersionHeader))
        {
            return ParseVersion(apiVersionHeader);
        }

        if (!headers.TryGetValue(HttpHeaders.Version, out StringValues versionHeader))
        {
            return Maybe<Version>.None;
        }

        return ParseVersion(versionHeader);
    }

    private static Maybe<Version> ParseVersion(StringValues values)
    {
        if (values.Count != 1)
        {
            return Maybe<Version>.None;
        }

        return Version.TryParse(values[0], out Version? version) ? version : Maybe<Version>.None;
    }
}
