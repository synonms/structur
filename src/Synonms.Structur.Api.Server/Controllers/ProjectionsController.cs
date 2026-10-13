using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Server.Controllers;

[ApiController]
public class ProjectionsController : ControllerBase
{
    public class ProjectionModel
    {
        public required Uri Href { get; init; }
        public string? Description { get; init; }
    }

    private readonly IResourceDirectory _resourceDirectory;
    private readonly IRouteGenerator _routeGenerator;

    public ProjectionsController(IResourceDirectory resourceDirectory, IRouteGenerator routeGenerator)
    {
        _resourceDirectory = resourceDirectory;
        _routeGenerator = routeGenerator;
    }

    [HttpGet]
    [Route("{collectionPath}/{id:guid}/projections")]
    public IActionResult GetAll([FromRoute] string collectionPath, [FromRoute] Guid id)
    {
        Dictionary<string, ProjectionModel> uris = new();

        IResourceDirectory.AggregateRootLayout? aggregateRootLayout = _resourceDirectory.GetAllRoots()
            .SingleOrDefault(layout => string.Equals(layout.CollectionPath, collectionPath, StringComparison.OrdinalIgnoreCase));

        if (aggregateRootLayout is not null)
        {
            foreach (Type projectionType in aggregateRootLayout.ProjectionTypes)
            {
                StructurProjectionAttribute? projectionAttribute = projectionType.GetCustomAttribute<StructurProjectionAttribute>();

                if (projectionAttribute is null)
                {
                    continue;
                }

                Uri projectionUri = _routeGenerator.Projection(aggregateRootLayout.AggregateRootType, aggregateRootLayout.ResourceType, projectionType, id);

                uris.Add(projectionAttribute.Name, new ProjectionModel { Href = projectionUri, Description = projectionAttribute.Description });
            }
        }

        return Ok(uris);
    }
}
