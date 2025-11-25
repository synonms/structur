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
public class PutEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly ICommandHandler<UpdateResourceCommand<TAggregateRoot, TResource>, UpdateResourceCommandResponse<TAggregateRoot>> _mediator;
    private readonly IRouteGenerator _routeGenerator;
    private readonly IErrorCollectionDocumentFactory _errorCollectionDocumentFactory;

    public PutEndpoint(ICommandHandler<UpdateResourceCommand<TAggregateRoot, TResource>, UpdateResourceCommandResponse<TAggregateRoot>> mediator, IRouteGenerator routeGenerator, IErrorCollectionDocumentFactory errorCollectionDocumentFactory)
    {
        _mediator = mediator;
        _routeGenerator = routeGenerator;
        _errorCollectionDocumentFactory = errorCollectionDocumentFactory;
    }

    [HttpPut]
    [Route("{id}")]
    public async Task<IActionResult> PutAsync([FromRoute] EntityId<TAggregateRoot> id, [FromBody] TResource resource)
    {
        // The Id should not be passed in the body so the resource.Id value will be a random Guid.
        // Align it so that if resource.Id is accessed during the request pipeline it is correct as per the route. 
        resource.Id = id.Value;
        
        EntityTag? ifMatch = HttpContext.Request.Headers.ExtractIfMatch();

        UpdateResourceCommand<TAggregateRoot, TResource> request = new(id, resource)
        {
            IfMatch = ifMatch
        };
        
        Result<UpdateResourceCommandResponse<TAggregateRoot>> commandResult = await _mediator.HandleAsync(request);
    
        return commandResult.Match(
            commandResponse =>
            {
                Response.Headers[HeaderNames.ETag] = commandResponse.AggregateRoot.EntityTag.ToString();    

                return StatusCode(StatusCodes.Status204NoContent);
            },
            fault =>
            {
                Uri itemUri = _routeGenerator.Item(id);
                Link requestedDocumentLink = new (itemUri, IanaLinkRelationConstants.Item, IanaHttpMethodConstants.Put);
                ErrorCollectionDocument errorCollectionDocument = _errorCollectionDocumentFactory.Create(fault, requestedDocumentLink);

                return HttpResponseMapper.MapFault(fault, errorCollectionDocument);
            });
    }
}