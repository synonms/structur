using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.WebApi.Cors;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Mediation.Queries;

namespace Synonms.Structur.WebApi.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class GetByIdEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IQueryHandler<FindResourceQuery<TAggregateRoot, TResource>, FindResourceQueryResponse<TAggregateRoot, TResource>> _queryHandler;

    public GetByIdEndpoint(IQueryHandler<FindResourceQuery<TAggregateRoot, TResource>,FindResourceQueryResponse<TAggregateRoot, TResource>> queryHandler)
    {
        _queryHandler = queryHandler;
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        EntityTag? ifNoneMatch = HttpContext.Request.Headers.ExtractIfNoneMatch();
        
        FindResourceQuery<TAggregateRoot, TResource> request = new(id)
        {
            IfNoneMatch = ifNoneMatch
        };
        
        Result<FindResourceQueryResponse<TAggregateRoot, TResource>> queryResult = await _queryHandler.HandleAsync(request);
    
        return queryResult.Match<IActionResult>(
            queryResponse =>
            {
                ResourceDocument<TResource> document = new(queryResponse.Resource.SelfLink, queryResponse.Resource);

                HttpContext.Response.Headers[HeaderNames.ETag] = queryResponse.EntityTag.ToString();     

                return Ok(document);
            },
            fault => HttpResponseMapper.MapFault(fault));
    }
}