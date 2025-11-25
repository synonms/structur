using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Routing;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Errors;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Cors;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Mediation.Commands;

namespace Synonms.Structur.WebApi.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class PostEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly ICommandHandler<CreateResourceCommand<TAggregateRoot, TResource>, CreateResourceCommandResponse<TAggregateRoot>> _commandHandler;
    private readonly IRouteGenerator _routeGenerator;
    private readonly IErrorCollectionDocumentFactory _errorCollectionDocumentFactory;

    public PostEndpoint(ICommandHandler<CreateResourceCommand<TAggregateRoot, TResource>, CreateResourceCommandResponse<TAggregateRoot>> commandHandler, IRouteGenerator routeGenerator, IErrorCollectionDocumentFactory errorCollectionDocumentFactory)
    {
        _commandHandler = commandHandler;
        _routeGenerator = routeGenerator;
        _errorCollectionDocumentFactory = errorCollectionDocumentFactory;
    }
    
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> PostAsync([FromBody] TResource resource)
    {
        CreateResourceCommand<TAggregateRoot, TResource> request = new(resource);
        Result<CreateResourceCommandResponse<TAggregateRoot>> commandResult = await _commandHandler.HandleAsync(request);

        return commandResult.Match(
            commandResponse =>
            {
                Response.Headers[HeaderNames.Location] = _routeGenerator.Item(commandResponse.AggregateRoot.Id).OriginalString;
                Response.Headers[HeaderNames.ETag] = commandResponse.AggregateRoot.EntityTag.ToString();    

                return StatusCode(StatusCodes.Status201Created);
            },
            fault =>
            {
                Uri collectionUri = _routeGenerator.Collection<TAggregateRoot>();
                Link requestedDocumentLink = new(collectionUri, IanaLinkRelationConstants.Collection, IanaHttpMethodConstants.Post);
                ErrorCollectionDocument errorCollectionDocument = _errorCollectionDocumentFactory.Create(fault, requestedDocumentLink);

                return HttpResponseMapper.MapFault(fault, errorCollectionDocument);
            });
    }
}