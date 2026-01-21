using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Tenants.Resolution;
using Xunit;

namespace Synonms.Structur.WebApi.Tests.Unit.Tenants.Resolution;

public class HeaderTenantIdResolutionStrategyTests
{
     private readonly IHttpContextAccessor _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
    [Fact]
    public void Resolve_CodeHeaderExistsWithSingleValue_ReturnsCode()
    {
        Guid expectedId = Guid.NewGuid();
            
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers[HttpHeaders.TenantId] = expectedId.ToString();
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        HeaderTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        result.Match(
            tenantId => Assert.Equal(expectedId, tenantId),
            () => Assert.Fail("Expected Some"));
    }
        
    [Fact]
    public void Resolve_CodeHeaderExistsWithMultipleValues_ReturnsNone()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers[HttpHeaders.TenantId] = new StringValues([Guid.NewGuid().ToString(), Guid.NewGuid().ToString()]);
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        HeaderTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Resolve_CodeHeaderExistsWithInvalidGuid_ReturnsNone()
    {
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers[HttpHeaders.TenantId] = "some-invalid-id";
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        HeaderTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Resolve_CodeHeaderDoesNotExist_ReturnsNone()
    {
        DefaultHttpContext httpContext = new();
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        HeaderTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        Assert.True(result.IsNone);
    }
}