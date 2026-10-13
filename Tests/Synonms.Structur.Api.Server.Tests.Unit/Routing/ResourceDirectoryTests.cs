using System.Reflection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Controllers;
using Synonms.Structur.Api.Server.Endpoints;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.Aggregates;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Routing;

public class ResourceDirectoryTests
{
    [Fact]
    public void GetAllRoots_GivenMultipleRepresentations_ReturnsEachRegistrationAndMetadata()
    {
        ResourceDirectory directory = new(typeof(MultiRepresentationAggregate).Assembly);

        IResourceDirectory.AggregateRootLayout[] layouts = directory.GetAllRoots()
            .Where(layout => layout.AggregateRootType == typeof(MultiRepresentationAggregate))
            .ToArray();

        Assert.Equal(2, layouts.Length);
        Assert.Equal(typeof(MultiRepresentationAdminResource), directory.GetDefaultRoot(typeof(MultiRepresentationAggregate)).ResourceType);
        Assert.Contains(layouts, layout =>
            layout.ResourceType == typeof(MultiRepresentationPublicResource)
            && layout.CollectionPath == "public-representations"
            && layout.Registration.IsCreateDisabled
            && layout.Registration.ReadPolicy == "ReadPublicRepresentation"
            && layout.SupportedApiVersions.SetEquals([new Version(1, 0), new Version(1, 1)]));
        Assert.Contains(layouts, layout =>
            layout.ResourceType == typeof(MultiRepresentationAdminResource)
            && layout.CollectionPath == "admin-representations"
            && !layout.Registration.IsCreateDisabled
            && layout.Registration.CreatePolicy == "CreateAdminRepresentation");
    }

    [Fact]
    public void GetRoot_GivenAggregateAndRepresentation_ReturnsExactRegistration()
    {
        ResourceDirectory directory = new(typeof(MultiRepresentationAggregate).Assembly);

        IResourceDirectory.AggregateRootLayout layout = directory.GetRoot(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationAdminResource));

        Assert.Equal("admin-representations", layout.CollectionPath);
    }

    [Fact]
    public void GetRoot_GivenUnregisteredPair_ThrowsDiagnostic()
    {
        ResourceDirectory directory = new(typeof(MultiRepresentationAggregate).Assembly);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            directory.GetRoot(typeof(MultiRepresentationAggregate), typeof(UnregisteredResource)));

        Assert.Contains("No resource registration", exception.Message);
    }

    [Fact]
    public void ControllerFeatureProvider_GivenRepresentations_RegistersOnlyConfiguredOperations()
    {
        ResourceDirectory directory = new(typeof(MultiRepresentationAggregate).Assembly);
        ControllerFeature feature = new();

        new Synonms.Structur.Api.Server.Controllers.ControllerFeatureProvider(directory)
            .PopulateFeature(Array.Empty<ApplicationPart>(), feature);

        Type[] controllers = feature.Controllers.Select(controller => controller.AsType()).ToArray();
        Assert.Contains(typeof(GetAllEndpoint<MultiRepresentationAggregate, MultiRepresentationPublicResource>), controllers);
        Assert.DoesNotContain(typeof(PostEndpoint<MultiRepresentationAggregate, MultiRepresentationPublicResource>), controllers);
        Assert.Contains(typeof(GetAllEndpoint<MultiRepresentationAggregate, MultiRepresentationAdminResource>), controllers);
        Assert.Contains(typeof(PostEndpoint<MultiRepresentationAggregate, MultiRepresentationAdminResource>), controllers);
    }

    [Fact]
    public void ControllerFeatureProvider_GivenReadAndFormCapabilities_RegistersOnlyEnabledOperations()
    {
        ResourceDirectory directory = new(typeof(MultiRepresentationAggregate).Assembly);
        ControllerFeature feature = new();

        new Synonms.Structur.Api.Server.Controllers.ControllerFeatureProvider(directory)
            .PopulateFeature(Array.Empty<ApplicationPart>(), feature);

        Type[] controllers = feature.Controllers.Select(controller => controller.AsType()).ToArray();
        Assert.DoesNotContain(typeof(GetAllEndpoint<WriteOnlyAggregate, WriteOnlyResource>), controllers);
        Assert.DoesNotContain(typeof(GetByIdEndpoint<WriteOnlyAggregate, WriteOnlyResource>), controllers);
        Assert.DoesNotContain(typeof(CreateFormEndpoint<WriteOnlyAggregate, WriteOnlyResource>), controllers);
        Assert.Contains(typeof(PostEndpoint<WriteOnlyAggregate, WriteOnlyResource>), controllers);
    }

    [Fact]
    public void DefaultResourceMapper_GivenMultipleRepresentations_MapsEachIndependentContract()
    {
        MultiRepresentationAggregate aggregate = MultiRepresentationAggregate.Create();
        IRouteGenerator routeGenerator = Substitute.For<IRouteGenerator>();
        routeGenerator.Item(Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<Guid>(), Arg.Any<QueryParameters>())
            .Returns(new Uri("/resources/123", UriKind.Relative));
        routeGenerator.EditForm(Arg.Any<Type>(), Arg.Any<Type>(), Arg.Any<Guid>(), Arg.Any<QueryParameters>())
            .Returns(new Uri("/resources/123/edit-form", UriKind.Relative));
        IResourceMapperFactory resourceMapperFactory = Substitute.For<IResourceMapperFactory>();
        IChildResourceMapperFactory childResourceMapperFactory = Substitute.For<IChildResourceMapperFactory>();
        DefaultResourceMapper<MultiRepresentationAggregate, MultiRepresentationPublicResource> publicMapper = new(
            NullLogger<DefaultResourceMapper<MultiRepresentationAggregate, MultiRepresentationPublicResource>>.Instance,
            resourceMapperFactory,
            childResourceMapperFactory,
            routeGenerator);
        DefaultResourceMapper<MultiRepresentationAggregate, MultiRepresentationAdminResource> adminMapper = new(
            NullLogger<DefaultResourceMapper<MultiRepresentationAggregate, MultiRepresentationAdminResource>>.Instance,
            resourceMapperFactory,
            childResourceMapperFactory,
            routeGenerator);

        MultiRepresentationPublicResource publicResource = publicMapper.Map(aggregate);
        MultiRepresentationAdminResource adminResource = adminMapper.Map(aggregate);

        Assert.Equal("Public", publicResource.PublicName);
        Assert.Equal("Internal", adminResource.InternalName);
    }

    [Fact]
    public void ControllerModelConvention_GivenAdminRepresentation_UsesItsRouteAndAuthorizationPolicy()
    {
        ResourceDirectory directory = new(typeof(MultiRepresentationAggregate).Assembly);
        Type controllerType = typeof(PostEndpoint<MultiRepresentationAggregate, MultiRepresentationAdminResource>);
        MethodInfo method = controllerType.GetMethod(nameof(PostEndpoint<MultiRepresentationAggregate, MultiRepresentationAdminResource>.PostAsync))!;
        ControllerModel controllerModel = new(controllerType.GetTypeInfo(), []);
        ActionModel actionModel = new(method, []) { ActionName = "Post" };
        controllerModel.Actions.Add(actionModel);

        new ControllerModelConvention(new RouteNameProvider(), directory).Apply(controllerModel);

        RouteAttribute route = Assert.Single(actionModel.Selectors).AttributeRouteModel!.Attribute as RouteAttribute
            ?? throw new InvalidOperationException("Expected a generated route attribute.");
        AuthorizeFilter authorization = Assert.Single(actionModel.Filters.OfType<AuthorizeFilter>());
        Assert.Equal("admin-representations", controllerModel.Selectors[0].AttributeRouteModel!.Template);
        Assert.Equal(string.Empty, route.Template);
        Assert.Equal("CreateAdminRepresentation", Assert.Single(authorization.AuthorizeData!).Policy);
    }

    [Fact]
    public void ValidateRootRegistrations_GivenDuplicateCollectionPath_ThrowsInsteadOfSelectingOne()
    {
        IResourceDirectory.AggregateRootLayout[] layouts =
        [
            CreateLayout(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationPublicResource), "same-path", true),
            CreateLayout(typeof(DuplicatePathAggregate), typeof(UnregisteredResource), "SAME-PATH", true)
        ];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ResourceDirectory.ValidateRootRegistrations(layouts));

        Assert.Contains("registered more than once", exception.Message);
    }

    [Fact]
    public void ValidateRootRegistrations_GivenDuplicateAggregateResourcePair_Throws()
    {
        IResourceDirectory.AggregateRootLayout[] layouts =
        [
            CreateLayout(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationPublicResource), "first", true),
            CreateLayout(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationPublicResource), "second", false)
        ];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ResourceDirectory.ValidateRootRegistrations(layouts));

        Assert.Contains("Duplicate resource registration", exception.Message);
    }

    [Fact]
    public void ValidateRootRegistrations_GivenOverlappingGeneratedRoutes_Throws()
    {
        IResourceDirectory.AggregateRootLayout[] layouts =
        [
            CreateLayout(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationPublicResource), "overlap", true),
            CreateLayout(typeof(DuplicatePathAggregate), typeof(UnregisteredResource), "overlap/create-form", true)
        ];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ResourceDirectory.ValidateRootRegistrations(layouts));

        Assert.Contains("Generated GET route", exception.Message);
        Assert.Contains("collides", exception.Message);
    }

    [Fact]
    public void ValidateRootRegistrations_GivenMultipleRepresentationsWithoutExactlyOneDefault_Throws()
    {
        IResourceDirectory.AggregateRootLayout[] layouts =
        [
            CreateLayout(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationPublicResource), "first", false),
            CreateLayout(typeof(MultiRepresentationAggregate), typeof(MultiRepresentationAdminResource), "second", false)
        ];

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            ResourceDirectory.ValidateRootRegistrations(layouts));

        Assert.Contains("exactly one as the default", exception.Message);
    }

    private static IResourceDirectory.AggregateRootLayout CreateLayout(Type aggregateType, Type resourceType, string path, bool isDefault) =>
        new(aggregateType, resourceType, path, new StructurResourceAttribute(resourceType, path), isDefault, new HashSet<Version>(), []);

}

[StructurResource(typeof(MultiRepresentationPublicResource), "public-representations", allowAnonymous: true, isCreateDisabled: true, isUpdateDisabled: true, isDeleteDisabled: true, readPolicy: "ReadPublicRepresentation", SupportedApiVersions = new[] { "1.0", "1.1" })]
[StructurResource(typeof(MultiRepresentationAdminResource), "admin-representations", createPolicy: "CreateAdminRepresentation", isDefaultRepresentation: true)]
public class MultiRepresentationAggregate : AggregateRoot<MultiRepresentationAggregate>
{
    private MultiRepresentationAggregate()
    {
    }

    public string PublicName { get; private set; } = string.Empty;
    public string InternalName { get; private set; } = string.Empty;

    public static MultiRepresentationAggregate Create() => new()
    {
        PublicName = "Public",
        InternalName = "Internal"
    };
}

public class MultiRepresentationPublicResource
{
    public string PublicName { get; set; } = string.Empty;
}

public class MultiRepresentationAdminResource
{
    public string InternalName { get; set; } = string.Empty;
}

public class UnregisteredResource
{
}

[StructurResource(typeof(WriteOnlyResource), "write-only", isReadDisabled: true, isCreateFormDisabled: true, isUpdateDisabled: true, isDeleteDisabled: true)]
public class WriteOnlyAggregate : AggregateRoot<WriteOnlyAggregate>
{
    private WriteOnlyAggregate()
    {
    }
}

public class WriteOnlyResource
{
}

public class DuplicatePathAggregate : AggregateRoot<DuplicatePathAggregate>
{
    private DuplicatePathAggregate()
    {
    }
}
