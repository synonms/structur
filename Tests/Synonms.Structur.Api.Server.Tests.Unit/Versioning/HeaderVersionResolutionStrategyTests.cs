using Microsoft.AspNetCore.Http;
using NSubstitute;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Server.Versioning.Resolution;
using Synonms.Structur.Core.Functional;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Versioning;

public class HeaderVersionResolutionStrategyTests
{
    [Fact]
    public void Resolve_GivenApiVersionHeader_ReturnsRequestedVersion()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers[HttpHeaders.ApiVersion] = "1.2";
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        HeaderVersionResolutionStrategy strategy = new(accessor);

        Maybe<Version> result = strategy.Resolve();

        result.Match(
            version => Assert.Equal(new Version(1, 2), version),
            () => Assert.Fail("Expected API version to be resolved."));
    }

    [Fact]
    public void Resolve_GivenLegacyVersionHeader_ReturnsRequestedVersion()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers[HttpHeaders.Version] = "1.0";
        IHttpContextAccessor accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        HeaderVersionResolutionStrategy strategy = new(accessor);

        Maybe<Version> result = strategy.Resolve();

        result.Match(
            version => Assert.Equal(new Version(1, 0), version),
            () => Assert.Fail("Expected legacy version to be resolved."));
    }
}
