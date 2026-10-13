using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Tests.Unit.Shared;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Mapping;

public class ResourceMappingPlanTests
{
    private readonly IRouteGenerator _routeGenerator = Substitute.For<IRouteGenerator>();
    private readonly IResourceMapperFactory _resourceMapperFactory = Substitute.For<IResourceMapperFactory>();
    private readonly IChildResourceMapperFactory _childResourceMapperFactory = Substitute.For<IChildResourceMapperFactory>();

    public ResourceMappingPlanTests()
    {
        _routeGenerator.Item(Arg.Any<EntityId<TestAggregateRoot>>()).Returns(new Uri("/tests/123", UriKind.Relative));
        _routeGenerator.Item(Arg.Any<Type>(), Arg.Any<Guid>(), Arg.Any<QueryParameters>()).Returns(new Uri("/tests/123", UriKind.Relative));
        _routeGenerator.Item(Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<Guid>(), Arg.Any<QueryParameters>()).Returns(new Uri("/tests/123", UriKind.Relative));
        _routeGenerator.Collection(Arg.Any<Type>(), Arg.Any<QueryParameters>()).Returns(new Uri("/tests", UriKind.Relative));
    }

    [Fact]
    public void MapChild_GivenAggregateMember_MapsPropertiesAndId()
    {
        TestAggregateMember member = TestAggregateMember.Create();
        DefaultChildResourceMapper<TestAggregateMember, TestChildResource> mapper = CreateChildMapper();

        TestChildResource resource = mapper.Map(member)!;

        Assert.Equal(member.Property1.Value, resource.Property1);
        Assert.Equal(member.Property2.Value, resource.Property2);
    }

    [Fact]
    public void Map_GivenUnmatchedDestinationProperty_PreservesConfiguredDefault()
    {
        DefaultResourceMapper<TestAggregateRoot, ResourceOnlyFieldResource> mapper = CreateMapper<ResourceOnlyFieldResource>();

        ResourceOnlyFieldResource resource = mapper.Map(TestAggregateRoot.Create());

        Assert.Equal("configured", resource.ResourceOnlyField);
    }

    [Fact]
    public void Map_GivenPositionalRecordContract_MapsInitOnlyProperties()
    {
        DefaultResourceMapper<TestAggregateRoot, PositionalRecordResource> mapper = CreateMapper<PositionalRecordResource>();

        PositionalRecordResource resource = mapper.Map(TestAggregateRoot.Create());

        Assert.Equal("Some string", resource.SomeString);
    }

    [Fact]
    public void Map_GivenRegisteredPropertyOverride_UsesRenamedSourceAndConverter()
    {
        ResourceMappingConfiguration.Register<TestAggregateRoot, RenamedPropertyResource>(
            nameof(RenamedPropertyResource.DisplayName),
            nameof(TestAggregateRoot.SomeString),
            value => ((Moniker)value!).Value);
        DefaultResourceMapper<TestAggregateRoot, RenamedPropertyResource> mapper = CreateMapper<RenamedPropertyResource>();

        RenamedPropertyResource resource = mapper.Map(TestAggregateRoot.Create());

        Assert.Equal("Some string", resource.DisplayName);
    }

    [Fact]
    public void Register_GivenMissingOverrideDestination_ThrowsPairDiagnostic()
    {
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => ResourceMappingConfiguration.Register<TestAggregateRoot, RenamedPropertyResource>(
                "MissingProperty",
                nameof(TestAggregateRoot.SomeString),
                value => value));

        Assert.Contains(typeof(TestAggregateRoot).FullName!, exception.Message);
        Assert.Contains(typeof(RenamedPropertyResource).FullName!, exception.Message);
        Assert.Contains("MissingProperty", exception.Message);
    }

    [Fact]
    public void Map_GivenIncompatibleMatchedProperty_ThrowsDiagnosticWithoutSourceValue()
    {
        DefaultResourceMapper<TestAggregateRoot, IncompatiblePropertyResource> mapper = CreateMapper<IncompatiblePropertyResource>();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => mapper.Map(TestAggregateRoot.Create()));

        Assert.Contains(typeof(TestAggregateRoot).FullName!, exception.Message);
        Assert.Contains(typeof(IncompatiblePropertyResource).FullName!, exception.Message);
        Assert.Contains(nameof(IncompatiblePropertyResource.SomeString), exception.Message);
        Assert.DoesNotContain("Some string", exception.ToString());
    }

    [Fact]
    public void MapConcurrently_GivenSamePair_ProducesConsistentResources()
    {
        TestAggregateRoot aggregate = TestAggregateRoot.Create();
        DefaultResourceMapper<TestAggregateRoot, TestResource> mapper = CreateMapper<TestResource>();
        ConcurrentBag<TestResource> resources = [];

        Parallel.For(0, 32, _ => resources.Add(mapper.Map(aggregate)));

        Assert.Equal(32, resources.Count);
        Assert.All(resources, resource => Assert.Equal(aggregate.SomeString.Value, resource.SomeString));
    }

    [Fact]
    public void ResourceMapperFactory_GivenDefaultAndCustomMapper_SelectsCustom()
    {
        CustomResourceMapper customMapper = new();
        ServiceCollection services = [];
        services.AddSingleton<IResourceMapper>(CreateMapper<TestResource>());
        services.AddSingleton<IResourceMapper>(customMapper);
        using ServiceProvider provider = services.BuildServiceProvider();
        ResourceMapperFactory factory = new(provider);

        IResourceMapper? selected = factory.Create(typeof(TestAggregateRoot), typeof(TestResource));

        Assert.Same(customMapper, selected);
    }

    [Fact]
    public void ResourceMapperFactory_GivenMultipleCustomMappers_ThrowsDiagnostic()
    {
        ServiceCollection services = [];
        services.AddSingleton<IResourceMapper>(new CustomResourceMapper());
        services.AddSingleton<IResourceMapper>(new SecondCustomResourceMapper());
        using ServiceProvider provider = services.BuildServiceProvider();
        ResourceMapperFactory factory = new(provider);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
            () => factory.Create(typeof(TestAggregateRoot), typeof(TestResource)));

        Assert.Contains(typeof(TestAggregateRoot).FullName!, exception.Message);
        Assert.Contains(typeof(TestResource).FullName!, exception.Message);
    }

    private DefaultResourceMapper<TestAggregateRoot, TResource> CreateMapper<TResource>()
        =>
        new(
            Substitute.For<ILogger<DefaultResourceMapper<TestAggregateRoot, TResource>>>(),
            _resourceMapperFactory,
            _childResourceMapperFactory,
            _routeGenerator);

    public sealed record PositionalRecordResource(string SomeString);

    private DefaultChildResourceMapper<TestAggregateMember, TestChildResource> CreateChildMapper() =>
        new(
            Substitute.For<ILogger<DefaultChildResourceMapper<TestAggregateMember, TestChildResource>>>(),
            _resourceMapperFactory,
            _childResourceMapperFactory,
            _routeGenerator);

    public class ResourceOnlyFieldResource
    {
        public string ResourceOnlyField { get; set; } = "configured";

    }

    public class RenamedPropertyResource
    {
        public string? DisplayName { get; set; }

    }

    public class IncompatiblePropertyResource
    {
        public int SomeString { get; set; }

    }

    private class CustomResourceMapper : IResourceMapper<TestAggregateRoot, TestResource>
    {
        public TestResource Map(TestAggregateRoot aggregateRoot) => throw new NotImplementedException();

        object? IResourceMapper.Map(object value) => throw new NotImplementedException();
    }

    private class SecondCustomResourceMapper : IResourceMapper<TestAggregateRoot, TestResource>
    {
        public TestResource Map(TestAggregateRoot aggregateRoot) => throw new NotImplementedException();

        object? IResourceMapper.Map(object value) => throw new NotImplementedException();
    }
}
