using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using NSubstitute;
using Synonms.Structur.Api.Server.Controllers;
using Synonms.Structur.Api.Server.Endpoints;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.Aggregates;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Routing;

public class ControllerModelConventionTests
{
    [Theory]
    [InlineData("Put", "none")]
    [InlineData("Put", "empty")]
    [InlineData("Put", "attribute")]
    [InlineData("Delete", "none")]
    [InlineData("Delete", "empty")]
    [InlineData("Delete", "attribute")]
    public void Apply_ItemMutation_UsesIdRouteAndRepresentationRouteName(string actionName, string selectorKind)
    {
        Type aggregateType = typeof(TestAggregate);
        Type resourceType = typeof(TestResource);
        Type controllerType = actionName == "Put"
            ? typeof(PutEndpoint<TestAggregate, TestResource>)
            : typeof(DeleteEndpoint<TestAggregate, TestResource>);
        MethodInfo method = controllerType.GetMethod(actionName + "Async")!;
        ControllerModel controller = new(controllerType.GetTypeInfo(), []);
        ActionModel action = new(method, []) { ActionName = actionName };
        controller.Actions.Add(action);

        if (selectorKind != "none")
        {
            action.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = selectorKind == "attribute"
                    ? new AttributeRouteModel(new RouteAttribute("{id}"))
                    : null
            });
        }

        IResourceDirectory directory = Substitute.For<IResourceDirectory>();
        StructurResourceAttribute registration = new(resourceType, "employees", allowAnonymous: true);
        directory.GetRoot(aggregateType, resourceType).Returns(
            new IResourceDirectory.AggregateRootLayout(aggregateType, resourceType, "employees", registration, true, new HashSet<Version>(), []));
        RouteNameProvider routeNames = new();

        new ControllerModelConvention(routeNames, directory).Apply(controller);

        AttributeRouteModel controllerRoute = Assert.Single(controller.Selectors).AttributeRouteModel!;
        AttributeRouteModel actionRoute = Assert.Single(action.Selectors).AttributeRouteModel!;
        Assert.Equal("{id}", actionRoute.Template);
        Assert.Equal("employees/{id}", AttributeRouteModel.CombineAttributeRouteModel(controllerRoute, actionRoute)!.Template);
        Assert.Equal(actionName == "Put"
            ? routeNames.Put(aggregateType, resourceType)
            : routeNames.Delete(aggregateType, resourceType), actionRoute.Name);
    }

    public class TestAggregate : AggregateRoot<TestAggregate>
    {
    }

    public class TestResource
    {
    }
}
