using NSubstitute;
using Synonms.Structur.Application.Tenants.Resolution;
using Synonms.Structur.Core.Functional;
using Xunit;

namespace Synonms.Structur.Application.Tests.Unit.Tenants.Resolution;

public class TenantIdResolverTests
{
    private readonly ITenantIdResolutionStrategy _strategy1 = Substitute.For<ITenantIdResolutionStrategy>();
    private readonly ITenantIdResolutionStrategy _strategy2 = Substitute.For<ITenantIdResolutionStrategy>();
    private readonly ITenantIdResolutionStrategy _strategy3 = Substitute.For<ITenantIdResolutionStrategy>();

    [Fact]
    public async Task ResolveAsync_AllSuccessfulStrategies_ReturnsFirstCode()
    {
        Guid expectedId = Guid.NewGuid();

        _strategy1.Resolve().Returns(Maybe<Guid>.Some(expectedId));
        _strategy2.Resolve().Returns(Maybe<Guid>.Some(Guid.NewGuid()));
        _strategy3.Resolve().Returns(Maybe<Guid>.Some(Guid.NewGuid()));
            
        List<ITenantIdResolutionStrategy> strategies = new()
        {
            _strategy1,
            _strategy2,
            _strategy3
        };
            
        TenantIdResolver idResolver = new(strategies);

        Maybe<Guid> result = await idResolver.ResolveAsync();

        result.Match(
            tenantId => Assert.Equal(expectedId, tenantId),
            () => Assert.Fail("Expected Some")
        );
    }
        
    [Fact]
    public async Task ResolveAsync_SomeSuccessfulStrategies_ReturnsFirstCode()
    {
        Guid expectedId = Guid.NewGuid();

        _strategy1.Resolve().Returns(Maybe<Guid>.None);
        _strategy2.Resolve().Returns(Maybe<Guid>.Some(expectedId));
        _strategy3.Resolve().Returns(Maybe<Guid>.Some(Guid.NewGuid()));
            
        List<ITenantIdResolutionStrategy> strategies = new()
        {
            _strategy1,
            _strategy2,
            _strategy3
        };
            
        TenantIdResolver idResolver = new(strategies);

        Maybe<Guid> result = await idResolver.ResolveAsync();

        result.Match(
            tenantId => Assert.Equal(expectedId, tenantId),
            () => Assert.Fail("Expected Some")
        );
    }
        
    [Fact]
    public async Task ResolveAsync_NoSuccessfulStrategies_ReturnsNone()
    {
        _strategy1.Resolve().Returns(Maybe<Guid>.None);
        _strategy2.Resolve().Returns(Maybe<Guid>.None);
        _strategy3.Resolve().Returns(Maybe<Guid>.None);
            
        List<ITenantIdResolutionStrategy> strategies = new()
        {
            _strategy1,
            _strategy2,
            _strategy3
        };
            
        TenantIdResolver idResolver = new(strategies);

        Maybe<Guid> result = await idResolver.ResolveAsync();

        Assert.True(result.IsNone);
    }
}