using System.Reflection;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Cors;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Mediation.Queries;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class GetAllEndpoint<TAggregateRoot, TResource> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IQueryHandler<ReadResourceCollectionQuery<TAggregateRoot, TResource>, ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>> _queryHandler;
    private readonly IRouteGenerator _routeGenerator;

    public GetAllEndpoint(IQueryHandler<ReadResourceCollectionQuery<TAggregateRoot, TResource>, ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>> queryHandler, IRouteGenerator routeGenerator)
    {
        _queryHandler = queryHandler;
        _routeGenerator = routeGenerator;
    }

    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetAllAsync([FromQuery] int offset = 0, [FromQuery] int limit = Pagination.DefaultPageLimit)
    {
        StructurResourceAttribute? resourceAttribute = typeof(TAggregateRoot).GetCustomAttribute<StructurResourceAttribute>();

        int configuredPageLimit = resourceAttribute?.PageLimit ?? 0;
        int pageLimit = configuredPageLimit > 0 ? configuredPageLimit : Math.Clamp(limit, 0, int.MaxValue);
        
        ReadResourceCollectionQuery<TAggregateRoot, TResource> request = new(pageLimit)
        {
            Offset = offset,
            QueryParameters = Request.Query.ExtractQueryParameters<TAggregateRoot>(),
            SortItems = Request.Query.ExtractSortItems()
        };
        Result<ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>> queryResult = await _queryHandler.HandleAsync(request);

        return queryResult.Match(
            queryResponse =>
            {
                Uri selfUri = _routeGenerator.Collection<TAggregateRoot>(request.QueryParameters);
                Link selfLink = Link.SelfLink(selfUri);
        
                Pagination pagination = queryResponse.ResourceCollection.GeneratePagination(o =>
                    _routeGenerator.Collection<TAggregateRoot>(request.QueryParameters)
                );

                ResourceCollectionDocument<TResource> document = new(selfLink, queryResponse.ResourceCollection, pagination);

                if (resourceAttribute?.IsCreateDisabled is false)
                {
                    Uri createFormUri = _routeGenerator.CreateForm<TAggregateRoot>();
                    Link createFormLink = Link.CreateFormLink(createFormUri);
                    document.WithLink(IanaLinkRelationConstants.Forms.Create, createFormLink);
                }

                return Ok(document) as IActionResult;
            }, 
            fault => HttpResponseMapper.MapFault(fault));
    }
}