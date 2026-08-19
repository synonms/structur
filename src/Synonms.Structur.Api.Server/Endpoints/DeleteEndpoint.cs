using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Mediation.Commands;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Errors;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class DeleteEndpoint<TAggregateRoot> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly ICommandHandler<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>> _commandHandler;
    private readonly IRouteGenerator _routeGenerator;
    private readonly IErrorCollectionDocumentFactory _errorCollectionDocumentFactory;

    public DeleteEndpoint(ICommandHandler<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>> commandHandler, IRouteGenerator routeGenerator, IErrorCollectionDocumentFactory errorCollectionDocumentFactory)
    {
        _commandHandler = commandHandler;
        _routeGenerator = routeGenerator;
        _errorCollectionDocumentFactory = errorCollectionDocumentFactory;
    }
    
    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        // TODO: Support parameters
        DeleteResourceCommand<TAggregateRoot> request = new(id);
        Result<DeleteResourceCommandResponse<TAggregateRoot>> response = await _commandHandler.HandleAsync(request);

        return response.Match<IActionResult>(
            _ => StatusCode(StatusCodes.Status200OK),
            fault =>
            {
                Uri itemUri = _routeGenerator.Item(id);
                Link requestedDocumentLink = new (itemUri, IanaLinkRelationConstants.Item, IanaHttpMethodConstants.Delete);
                ErrorCollectionDocument errorCollectionDocument = _errorCollectionDocumentFactory.Create(fault, requestedDocumentLink);

                return HttpResponseMapper.MapFault(fault, errorCollectionDocument);
            });
    }
}