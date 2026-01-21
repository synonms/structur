using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NSubstitute;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Tenants.Resolution;
using Xunit;

namespace Synonms.Structur.WebApi.Tests.Unit.Tenants.Resolution;

public class QueryStringTenantIdResolutionStrategyTests
{
    private readonly IHttpContextAccessor _mockHttpContextAccessor = Substitute.For<IHttpContextAccessor>();
        
    [Fact]
    public void Resolve_HeaderExistsWithSingleValue_ReturnsCode()
    {
        Guid expectedId = Guid.NewGuid();
            
        DefaultHttpContext httpContext = new()
        {
            Request =
            {
                QueryString = new QueryString($"?{HttpQueryStringKeys.TenantId}={expectedId}")
            }
        };
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        QueryStringTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        result.Match(
            tenantId => Assert.Equal(expectedId, tenantId),
            () => Assert.Fail("Expected Some")); 
    }
        
    [Fact]
    public void Resolve_HeaderExistsWithMultipleValues_ReturnsNone()
    {
        StringValues stringValues = new(new[]
        {
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString()
        });
            
        DefaultHttpContext httpContext = new()
        {
            Request =
            {
                QueryString = new QueryString($"?{HttpQueryStringKeys.TenantId}={stringValues}")
            }
        };
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        QueryStringTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Resolve_HeaderExistsWithInvalidCharacters_ReturnsNone()
    {
        DefaultHttpContext httpContext = new()
        {
            Request =
            {
                QueryString = new QueryString($"?{HttpQueryStringKeys.TenantId}=some-invalid-code")
            }
        };
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        QueryStringTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        Assert.True(result.IsNone);
    }

    [Fact]
    public void Resolve_HeaderDoesNotExist_ReturnsNone()
    {
        DefaultHttpContext httpContext = new();
        _mockHttpContextAccessor.HttpContext.Returns(httpContext);

        QueryStringTenantIdResolutionStrategy strategy = new(_mockHttpContextAccessor);
            
        Maybe<Guid> result = strategy.Resolve();

        Assert.True(result.IsNone);
    }
}