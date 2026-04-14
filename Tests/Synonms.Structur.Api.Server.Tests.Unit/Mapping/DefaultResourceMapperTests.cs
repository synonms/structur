using Microsoft.Extensions.Logging;
using NSubstitute;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Tests.Unit.Shared;
using Synonms.Structur.Core.Entities;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Mapping;

public class DefaultResourceMapperTests
{
    private readonly ILogger<DefaultResourceMapper<TestAggregateRoot, TestResource>> _mockLogger = Substitute.For<ILogger<DefaultResourceMapper<TestAggregateRoot, TestResource>>>();
    private readonly IRouteGenerator _mockRouteGenerator = Substitute.For<IRouteGenerator>();
    private readonly IResourceMapperFactory _mockResourceMapperFactory = Substitute.For<IResourceMapperFactory>();
    private readonly IResourceMapper<TestAggregateRoot, TestResource> _mockResourceMapper = Substitute.For<IResourceMapper<TestAggregateRoot, TestResource>>();
    private readonly IChildResourceMapperFactory _mockChildResourceMapperFactory = Substitute.For<IChildResourceMapperFactory>();

    public DefaultResourceMapperTests()
    {
        _mockResourceMapperFactory.Create(typeof(TestAggregateRoot), typeof(TestResource)).Returns(_mockResourceMapper);
    }
    
    [Fact]
    public void Map_GivenAggregate_ThenReturnsPopulatedResource()
    {
        TestAggregateRoot aggregateRoot = new(EntityId<TestAggregateRoot>.New(), TestUser.UserAction);
        
        DefaultResourceMapper<TestAggregateRoot, TestResource> mapper = new(_mockLogger, _mockResourceMapperFactory, _mockChildResourceMapperFactory, _mockRouteGenerator);

        TestResource resource = mapper.Map(aggregateRoot);
    }
}