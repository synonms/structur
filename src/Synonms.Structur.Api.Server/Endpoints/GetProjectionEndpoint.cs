using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Mediation.Queries;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Api.Server.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class GetProjectionEndpoint<TAggregateRoot, TProjection> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TProjection : Projection<TAggregateRoot>
{
    private readonly IQueryHandler<GetProjectionQuery<TAggregateRoot, TProjection>, GetProjectionQueryResponse<TAggregateRoot, TProjection>> _queryHandler;
    private readonly IRouteGenerator _routeGenerator;

    public GetProjectionEndpoint(IQueryHandler<GetProjectionQuery<TAggregateRoot, TProjection>, GetProjectionQueryResponse<TAggregateRoot, TProjection>> queryHandler, IRouteGenerator routeGenerator)
    {
        _queryHandler = queryHandler;
        _routeGenerator = routeGenerator;
    }

    [HttpGet]
    [Route("{id}/projections/{projectionIdentifier}")]
    public async Task<IActionResult> GetProjectionAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        EntityTag? ifNoneMatch = HttpContext.Request.Headers.ExtractIfNoneMatch();
        
        GetProjectionQuery<TAggregateRoot, TProjection> request = new(id)
        {
            IfNoneMatch = ifNoneMatch
        };
        
        Result<GetProjectionQueryResponse<TAggregateRoot, TProjection>> queryResult = await _queryHandler.HandleAsync(request);
    
        return queryResult.Match<IActionResult>(
            queryResponse =>
            {
                Uri projectionUri = _routeGenerator.Projection<TAggregateRoot, TProjection>(id);
                Link projectionLink = Link.SelfLink(projectionUri);
                ResourceDocument<TProjection> document = new(projectionLink, queryResponse.Projection);

                // TODO: Get ETag from aggregate
//                HttpContext.Response.Headers[HeaderNames.ETag] = queryResponse.EntityTag.ToString();     

                return Ok(document);
            },
            fault => HttpResponseMapper.MapFault(fault));
    }
}