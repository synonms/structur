using Microsoft.Extensions.Logging;
using NSubstitute;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Pipeline;
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
    private readonly IChildResourceMapperFactory _mockChildResourceMapperFactory = Substitute.For<IChildResourceMapperFactory>();

    public DefaultResourceMapperTests()
    {
        _mockRouteGenerator.Item(Arg.Any<Type>(), Arg.Any<Guid>(), Arg.Any<QueryParameters>()).Returns(new Uri("/tests/123", UriKind.Relative));
        _mockRouteGenerator.Item(Arg.Any<EntityId<TestAggregateRoot>>()).Returns(new Uri("/tests/123", UriKind.Relative));
        _mockRouteGenerator.Collection(Arg.Any<Type>(), Arg.Any<QueryParameters>()).Returns(new Uri("/tests", UriKind.Relative));
        
        DefaultResourceMapper<TestAggregateRoot, TestResource> resourceMapper = new(_mockLogger, _mockResourceMapperFactory, _mockChildResourceMapperFactory, _mockRouteGenerator);
        DefaultChildResourceMapper<TestAggregateMember, TestChildResource> childResourceMapper = new(Substitute.For<ILogger<DefaultChildResourceMapper<TestAggregateMember, TestChildResource>>>(), _mockResourceMapperFactory, _mockChildResourceMapperFactory, _mockRouteGenerator);

        _mockResourceMapperFactory.Create(typeof(TestAggregateRoot), typeof(TestResource)).Returns(resourceMapper);
        _mockChildResourceMapperFactory.Create(typeof(TestAggregateMember), typeof(TestChildResource)).Returns(childResourceMapper);
    }
    
    [Fact]
    public void Map_GivenAggregate_ThenReturnsPopulatedResource()
    {
        TestAggregateRoot aggregateRoot = TestAggregateRoot.Create();
        
        DefaultResourceMapper<TestAggregateRoot, TestResource> mapper = new(_mockLogger, _mockResourceMapperFactory, _mockChildResourceMapperFactory, _mockRouteGenerator);

        TestResource resource = mapper.Map(aggregateRoot);
        
        Assert.Equal(aggregateRoot.Id.Value, resource.Id);
        Assert.Equal("/tests/123", resource.SelfLink.Uri.ToString());
        Assert.Equal(aggregateRoot.SomeRelatedAggregateId.Value, resource.SomeRelatedAggregateId);
        Assert.Equal(aggregateRoot.SomeString.Value, resource.SomeString);
        Assert.Equal(aggregateRoot.SomeInt.Value, resource.SomeInt);
        Assert.Equal(aggregateRoot.SomeBool, resource.SomeBool);
        Assert.Equal(aggregateRoot.SomeList.Select(x => x.Value), resource.SomeList);
        Assert.Equal(aggregateRoot.SomeDate.Value, resource.SomeDate);
        Assert.Equal(aggregateRoot.SomeDateTime.Value, resource.SomeDateTime);
        Assert.Equal(aggregateRoot.ChildResource.Property1, resource.ChildResource?.Property1);
        Assert.Equal(aggregateRoot.ChildResource.Property2, resource.ChildResource?.Property2);
        Assert.Equal(aggregateRoot.ChildResources.Count, resource.ChildResources?.Count);
        for (int i = 0; i < aggregateRoot.ChildResources.Count; i++)        {
            Assert.Equal(aggregateRoot.ChildResources[i].Property1, resource.ChildResources?[i].Property1);
            Assert.Equal(aggregateRoot.ChildResources[i].Property2, resource.ChildResources?[i].Property2);
        }
    }
}