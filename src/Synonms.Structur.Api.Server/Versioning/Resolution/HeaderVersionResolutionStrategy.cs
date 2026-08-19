using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Versioning.Resolution;

public class HeaderVersionResolutionStrategy : IVersionResolutionStrategy
{
    private static readonly Func<KeyValuePair<string, StringValues>, bool> HeaderPredicate =
        header => header.Key.Equals(HttpHeaders.Version, StringComparison.OrdinalIgnoreCase);

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

        if (_httpContextAccessor.HttpContext.Request.Headers.Count(HeaderPredicate) != 1)
        {
            return Maybe<Version>.None;
        }

        KeyValuePair<string, StringValues> versionHeader = _httpContextAccessor.HttpContext.Request.Headers.Single(HeaderPredicate);

        if (versionHeader.Value.Count != 1)
        {
            return Maybe<Version>.None;
        }

        string? versionAsString = versionHeader.Value.SingleOrDefault();

        return Version.TryParse(versionAsString, out Version? version) ? version : Maybe<Version>.None;
    }
}