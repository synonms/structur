using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.WebApi.Endpoints;
using Synonms.Structur.WebApi.Routing;

namespace Synonms.Structur.WebApi.Controllers;

public class ControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
{
    private readonly IResourceDirectory _resourceDirectory;

    public ControllerFeatureProvider(IResourceDirectory resourceDirectory)
    {
        _resourceDirectory = resourceDirectory;
    }
    
    public void PopulateFeature(IEnumerable<ApplicationPart> parts, ControllerFeature feature)
    {
        foreach ((string _, IResourceDirectory.AggregateRootLayout aggregateLayout) in _resourceDirectory.GetAllRoots())
        {
            StructurResourceAttribute? resourceAttribute = aggregateLayout.AggregateRootType.GetCustomAttribute<StructurResourceAttribute>();

            if (resourceAttribute is null)
            {
                continue;
            }
            
            AddGetById(feature, aggregateLayout);
            AddGetAll(feature, aggregateLayout);

            if (resourceAttribute.IsCreateDisabled is false)
            {
                AddCreateForm(feature, aggregateLayout);
                AddPost(feature, aggregateLayout);
            }

            if (resourceAttribute.IsUpdateDisabled is false)
            {
                AddEditForm(feature, aggregateLayout);
                AddPut(feature, aggregateLayout);
            }

            if (resourceAttribute.IsDeleteDisabled is false)
            {
                AddDelete(feature, aggregateLayout);
            }
        }
    }

    private static void AddDelete(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type deleteEndpointType = typeof(DeleteEndpoint<>).MakeGenericType(aggregateRootLayout.AggregateRootType);
                
        feature.Controllers.Add(deleteEndpointType.GetTypeInfo());
    }

    private static void AddGetById(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type getByIdEndpointType = typeof(GetByIdEndpoint<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
                
        feature.Controllers.Add(getByIdEndpointType.GetTypeInfo());
    }
    
    private static void AddGetAll(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type getAllEndpointType = typeof(GetAllEndpoint<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
                
        feature.Controllers.Add(getAllEndpointType.GetTypeInfo());
    }

    private static void AddPost(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type postEndpointType = typeof(PostEndpoint<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
                
        feature.Controllers.Add(postEndpointType.GetTypeInfo());
    }
    
    private static void AddPut(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type putEndpointType = typeof(PutEndpoint<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
                
        feature.Controllers.Add(putEndpointType.GetTypeInfo());
    }
    
    private static void AddCreateForm(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type createFormEndpointType = typeof(CreateFormEndpoint<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
                
        feature.Controllers.Add(createFormEndpointType.GetTypeInfo());
    }
    
    private static void AddEditForm(ControllerFeature feature, IResourceDirectory.AggregateRootLayout aggregateRootLayout)
    {
        Type editFormEndpointType = typeof(EditFormEndpoint<,>).MakeGenericType(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType);
                
        feature.Controllers.Add(editFormEndpointType.GetTypeInfo());
    }
}