using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Api.Core.Faults;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Mediation.Commands;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Errors;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Endpoints;

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
    public async Task<IActionResult> PostAsync([FromBody] TResource? resource)
    {
        Uri collectionUri = _routeGenerator.Collection<TAggregateRoot>();
        Link requestedDocumentLink = new(collectionUri, IanaLinkRelationConstants.Collection, IanaHttpMethodConstants.Post);

        if (resource is null)
        {
            ClientFault fault = new("Unable to parse resource from request.");  
            ErrorCollectionDocument errorCollectionDocument = _errorCollectionDocumentFactory.Create(fault, requestedDocumentLink);

            return BadRequest(errorCollectionDocument);
        }
        
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
                ErrorCollectionDocument errorCollectionDocument = _errorCollectionDocumentFactory.Create(fault, requestedDocumentLink);

                return HttpResponseMapper.MapFault(fault, errorCollectionDocument);
            });
    }
}