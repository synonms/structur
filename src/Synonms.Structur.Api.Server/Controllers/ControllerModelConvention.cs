using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Server.Controllers;

public class ControllerModelConvention : IControllerModelConvention
{
    private readonly IRouteNameProvider _routeNameProvider;

    public ControllerModelConvention(IRouteNameProvider routeNameProvider)
    {
        _routeNameProvider = routeNameProvider;
    }
    
    public void Apply(ControllerModel controllerModel)
    {
        if (controllerModel.ControllerType.IsGenericType)
        {
            Type aggregateRootType = controllerModel.ControllerType.GenericTypeArguments[0];
            StructurResourceAttribute? resourceAttribute = aggregateRootType.GetCustomAttribute<StructurResourceAttribute>();

            if (resourceAttribute is null)
            {
                return;
            }
            
            AddControllerRoute(controllerModel, resourceAttribute);
            AddActionRoutes(controllerModel, aggregateRootType, resourceAttribute);
        }
    }

    private void AddActionRoutes(ControllerModel controllerModel, Type aggregateRootType, StructurResourceAttribute resourceAttribute)
    {
        foreach (ActionModel action in controllerModel.Actions)
        {
            RouteAttribute routeAttribute = action.ActionName switch
            {
                "GetById" => new RouteAttribute("{id}")
                {
                    Name = _routeNameProvider.GetById(aggregateRootType)
                },
                "GetAll" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.GetAll(aggregateRootType)
                },
                "Post" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.Post(aggregateRootType)
                },
                "Put" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.Put(aggregateRootType)
                },
                "Delete" => new RouteAttribute("")
                {
                    Name = _routeNameProvider.Delete(aggregateRootType)
                },
                "CreateForm" => new RouteAttribute(IanaLinkRelationConstants.Forms.Create)
                {
                    Name = _routeNameProvider.CreateForm(aggregateRootType)
                },
                "EditForm" => new RouteAttribute("{id}/" + IanaLinkRelationConstants.Forms.Edit)
                {
                    Name = _routeNameProvider.EditForm(aggregateRootType)
                },
                "GetProjection" => BuildProjectionRoute(controllerModel, aggregateRootType, resourceAttribute),
                _ => throw new InvalidOperationException($"Unexpected controller action '{action.ActionName}'.")
            };
            
            action.Selectors.Add(new SelectorModel
            {
                AttributeRouteModel = new AttributeRouteModel(routeAttribute)
            });

            if (resourceAttribute.AllowAnonymous)
            {
                AllowAnonymousFilter anonymousFilter = new();
                action.Filters.Add(anonymousFilter);
            }
            else
            {
                string policySuffix = aggregateRootType.Name;
                AuthorizeFilter authorizeFilter = action.ActionName switch
                {
                    "GetById" => new AuthorizeFilter("Read" + policySuffix),
                    "GetAll" => new AuthorizeFilter("Read" + policySuffix),
                    "Post" => new AuthorizeFilter("Create" + policySuffix),
                    "Put" => new AuthorizeFilter("Update" + policySuffix),
                    "Delete" => new AuthorizeFilter("Delete" + policySuffix),
                    "CreateForm" => new AuthorizeFilter("Create" + policySuffix),
                    "EditForm" => new AuthorizeFilter("Update" + policySuffix),
                    "GetProjection" => new AuthorizeFilter("Read" + policySuffix),
                    _ => throw new InvalidOperationException($"Unexpected controller action '{action.ActionName}'.")
                };
                
                action.Filters.Add(authorizeFilter);
            }
        }
    }

    private RouteAttribute BuildProjectionRoute(ControllerModel controllerModel, Type aggregateRootType, StructurResourceAttribute resourceAttribute)
    {
        if (controllerModel.ControllerType.GenericTypeArguments.Length < 2)
        {
            throw new InvalidOperationException($"Unable to determine projection type for controller '{controllerModel.ControllerName}'.");
        }
        
        Type projectionType = controllerModel.ControllerType.GenericTypeArguments[1];
        StructurProjectionAttribute? projectionAttribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();

        if (projectionAttribute is null)
        {
            throw new InvalidOperationException($"Projection attribute not found on projection type '{projectionType.Name}'.");
        }

        return new RouteAttribute("{id}/projections/" + projectionAttribute.ProjectionIdentifier)
        {
            Name = _routeNameProvider.GetProjection(aggregateRootType, projectionType)
        };
    }
    
    private static void AddControllerRoute(ControllerModel controllerModel, StructurResourceAttribute resourceAttribute)
    {
        if (string.IsNullOrWhiteSpace(resourceAttribute.CollectionPath) is false)
        {
            controllerModel.Selectors.Add(new SelectorModel
            {
                // TODO: Make base path configurable
                AttributeRouteModel = new AttributeRouteModel(new RouteAttribute(resourceAttribute.CollectionPath)),
            });
        }

        if (resourceAttribute.AllowAnonymous is false)
        {
            controllerModel.Filters.Add(new AuthorizeFilter());
        }
    }
}