using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Api.Server.Versioning;

namespace Synonms.Structur.Api.Server.Controllers;

public class ControllerModelConvention : IControllerModelConvention
{
    private readonly IRouteNameProvider _routeNameProvider;
    private readonly IResourceDirectory _resourceDirectory;

    public ControllerModelConvention(IRouteNameProvider routeNameProvider, IResourceDirectory resourceDirectory)
    {
        _routeNameProvider = routeNameProvider;
        _resourceDirectory = resourceDirectory;
    }

    public void Apply(ControllerModel controllerModel)
    {
        if (controllerModel.ControllerType.IsGenericType)
        {
            Type aggregateRootType = controllerModel.ControllerType.GenericTypeArguments[0];
            Type resourceType = controllerModel.ControllerType.GenericTypeArguments[1];
            IResourceDirectory.AggregateRootLayout layout = _resourceDirectory.GetRoot(aggregateRootType, resourceType);
            StructurResourceAttribute resourceAttribute = layout.Registration;

            AddControllerRoute(controllerModel, layout);
            AddActionRoutes(controllerModel, layout);
            if (layout.SupportedApiVersions.Count > 0)
            {
                controllerModel.Filters.Add(new RegistrationVersionFilter(resourceType, layout.SupportedApiVersions));
            }
        }
    }

    private void AddActionRoutes(ControllerModel controllerModel, IResourceDirectory.AggregateRootLayout layout)
    {
        Type aggregateRootType = layout.AggregateRootType;
        Type resourceType = layout.ResourceType;
        StructurResourceAttribute resourceAttribute = layout.Registration;

        foreach (ActionModel action in controllerModel.Actions)
        {
            RouteAttribute routeAttribute = action.ActionName switch
            {
                "GetById" => new RouteAttribute("{id}")
                {
                    Name = _routeNameProvider.GetById(aggregateRootType, resourceType)
                },
                "GetAll" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.GetAll(aggregateRootType, resourceType)
                },
                "Post" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.Post(aggregateRootType, resourceType)
                },
                "Put" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.Put(aggregateRootType, resourceType)
                },
                "Delete" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.Delete(aggregateRootType, resourceType)
                },
                "CreateForm" => new RouteAttribute(IanaLinkRelationConstants.Forms.Create)
                {
                    Name = _routeNameProvider.CreateForm(aggregateRootType, resourceType)
                },
                "EditForm" => new RouteAttribute("{id}/" + IanaLinkRelationConstants.Forms.Edit)
                {
                    Name = _routeNameProvider.EditForm(aggregateRootType, resourceType)
                },
                "GetProjection" => BuildProjectionRoute(controllerModel, layout),
                _ => throw new InvalidOperationException($"Unexpected controller action '{action.ActionName}'.")
            };

            if (action.Selectors.Count == 0)
            {
                action.Selectors.Add(new SelectorModel
                {
                    AttributeRouteModel = new AttributeRouteModel(routeAttribute)
                });
            }
            else
            {
                foreach (SelectorModel selector in action.Selectors)
                {
                    if (selector.AttributeRouteModel is null)
                    {
                        selector.AttributeRouteModel = new AttributeRouteModel(routeAttribute);
                    }
                    else
                    {
                        selector.AttributeRouteModel.Template = routeAttribute.Template;
                        selector.AttributeRouteModel.Name = routeAttribute.Name;
                    }
                }
            }

            if (resourceAttribute.AllowAnonymous)
            {
                AllowAnonymousFilter anonymousFilter = new();
                action.Filters.Add(anonymousFilter);
            }
            else
            {
                AuthorizeFilter authorizeFilter = action.ActionName switch
                {
                    "GetById" => new AuthorizeFilter(resourceAttribute.ReadPolicy ?? "Read" + aggregateRootType.Name),
                    "GetAll" => new AuthorizeFilter(resourceAttribute.ReadPolicy ?? "Read" + aggregateRootType.Name),
                    "Post" => new AuthorizeFilter(resourceAttribute.CreatePolicy ?? "Create" + aggregateRootType.Name),
                    "Put" => new AuthorizeFilter(resourceAttribute.UpdatePolicy ?? "Update" + aggregateRootType.Name),
                    "Delete" => new AuthorizeFilter(resourceAttribute.DeletePolicy ?? "Delete" + aggregateRootType.Name),
                    "CreateForm" => new AuthorizeFilter(resourceAttribute.CreatePolicy ?? "Create" + aggregateRootType.Name),
                    "EditForm" => new AuthorizeFilter(resourceAttribute.UpdatePolicy ?? "Update" + aggregateRootType.Name),
                    "GetProjection" => new AuthorizeFilter(resourceAttribute.ReadPolicy ?? "Read" + aggregateRootType.Name),
                    _ => throw new InvalidOperationException($"Unexpected controller action '{action.ActionName}'.")
                };

                action.Filters.Add(authorizeFilter);
            }
        }
    }

    private RouteAttribute BuildProjectionRoute(ControllerModel controllerModel, IResourceDirectory.AggregateRootLayout layout)
    {
        if (controllerModel.ControllerType.GenericTypeArguments.Length < 2)
        {
            throw new InvalidOperationException($"Unable to determine projection type for controller '{controllerModel.ControllerName}'.");
        }

        Type projectionType = controllerModel.ControllerType.GenericTypeArguments[2];
        StructurProjectionAttribute? projectionAttribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();

        if (projectionAttribute is null)
        {
            throw new InvalidOperationException($"Projection attribute not found on projection type '{projectionType.Name}'.");
        }

        return new RouteAttribute("{id}/projections/" + projectionAttribute.ProjectionIdentifier)
        {
            Name = _routeNameProvider.GetProjection(layout.AggregateRootType, layout.ResourceType, projectionType)
        };
    }

    private static void AddControllerRoute(ControllerModel controllerModel, IResourceDirectory.AggregateRootLayout layout)
    {
        StructurResourceAttribute resourceAttribute = layout.Registration;
        if (string.IsNullOrWhiteSpace(layout.CollectionPath) is false)
        {
            controllerModel.Selectors.Add(new SelectorModel
            {
                // TODO: Make base path configurable
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(layout.CollectionPath)),
            });
        }

    }
}
