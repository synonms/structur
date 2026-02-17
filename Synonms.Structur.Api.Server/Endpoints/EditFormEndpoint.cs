using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Mediation.Queries;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Forms;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class EditFormEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new()
{
    private readonly IQueryHandler<FindResourceQuery<TAggregateRoot, TResource>, FindResourceQueryResponse<TAggregateRoot, TResource>> _queryHandler;
    private readonly IRouteGenerator _routeGenerator;
    private readonly IEditFormDocumentFactory<TAggregateRoot, TResource> _documentFactory;

    public EditFormEndpoint(IQueryHandler<FindResourceQuery<TAggregateRoot, TResource>, FindResourceQueryResponse<TAggregateRoot, TResource>> queryHandler, IRouteGenerator routeGenerator, IEditFormDocumentFactory<TAggregateRoot, TResource> documentFactory)
    {
        _queryHandler = queryHandler;
        _routeGenerator = routeGenerator;
        _documentFactory = documentFactory;
    }
    
    [HttpGet]
    [Route("{id}/" + IanaLinkRelationConstants.Forms.Edit)]
    public async Task<IActionResult> EditFormAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        FindResourceQuery<TAggregateRoot, TResource> request = new(id);
        Result<FindResourceQueryResponse<TAggregateRoot, TResource>> queryResult = await _queryHandler.HandleAsync(request);

        return queryResult.Match<IActionResult>(
            queryResponse =>
            {
                Uri editFormUri = _routeGenerator.EditForm<TAggregateRoot>(id);
                Uri targetUri = _routeGenerator.Item<TAggregateRoot>(id);
                FormDocument document = _documentFactory.Create(editFormUri, targetUri, queryResponse.Resource);

                return Ok(document);
            },
            fault => HttpResponseMapper.MapFault(fault));
    }
}