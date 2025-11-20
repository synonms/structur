using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema.Forms;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Cors;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Mediation.Queries;
using Synonms.Structur.WebApi.Routing;

namespace Synonms.Structur.WebApi.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class EditFormEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new()
{
    private readonly IMediator _mediator;
    private readonly IRouteGenerator _routeGenerator;
    private readonly IEditFormDocumentFactory<TAggregateRoot, TResource> _documentFactory;

    public EditFormEndpoint(IMediator mediator, IRouteGenerator routeGenerator, IEditFormDocumentFactory<TAggregateRoot, TResource> documentFactory)
    {
        _mediator = mediator;
        _routeGenerator = routeGenerator;
        _documentFactory = documentFactory;
    }
    
    [HttpGet]
    [Route("{id}/" + IanaLinkRelationConstants.Forms.Edit)]
    public async Task<IActionResult> EditFormAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        FindResourceQuery<TAggregateRoot, TResource> request = new(id);
        Result<FindResourceQueryResponse<TAggregateRoot, TResource>> queryResult = await _mediator.SendQueryAsync<FindResourceQuery<TAggregateRoot, TResource>, FindResourceQueryResponse<TAggregateRoot, TResource>>(request);

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