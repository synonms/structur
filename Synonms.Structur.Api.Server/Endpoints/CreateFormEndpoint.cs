using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Forms;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class CreateFormEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new()
{
    private readonly IRouteGenerator _routeGenerator;
    private readonly ICreateFormDocumentFactory<TAggregateRoot, TResource> _documentFactory;

    public CreateFormEndpoint(IRouteGenerator routeGenerator, ICreateFormDocumentFactory<TAggregateRoot, TResource> documentFactory)
    {
        _routeGenerator = routeGenerator;
        _documentFactory = documentFactory;
    }
    
    [HttpGet]
    [Route(IanaLinkRelationConstants.Forms.Create)]
    public Task<IActionResult> CreateFormAsync()
    {
        Uri createFormUri = _routeGenerator.CreateForm<TAggregateRoot>();
        Uri targetUri = _routeGenerator.Collection<TAggregateRoot>();
        TResource resource = new();
        FormDocument document = _documentFactory.Create(createFormUri, targetUri, resource);

        return Task.FromResult(Ok(document) as IActionResult);
    }
}