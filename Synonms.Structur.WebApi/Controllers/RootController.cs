using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Application.Routing;

namespace Synonms.Structur.WebApi.Controllers;

[ApiController]
[Route("")]
public class RootController : ControllerBase
{
    private readonly IResourceDirectory _resourceDirectory;
    private readonly IRouteGenerator _routeGenerator;

    public RootController(IResourceDirectory resourceDirectory, IRouteGenerator routeGenerator)
    {
        _resourceDirectory = resourceDirectory;
        _routeGenerator = routeGenerator;
    }

    [HttpGet]
    public IActionResult Get()
    {
        Dictionary<string, Uri> uris = new();

        foreach ((string collectionRoute, IResourceDirectory.AggregateRootLayout aggregateRootLayout) in _resourceDirectory.GetAllRoots())
        {
            Uri getAllUri = _routeGenerator.Collection(aggregateRootLayout.AggregateRootType);
            
            uris.Add(collectionRoute, getAllUri);
        }

        return Ok(uris);
    }
}