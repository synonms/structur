using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class GetProjectionQueryResponse<TAggregateRoot, TProjection> : QueryResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TProjection : Projection<TAggregateRoot>
{
    public GetProjectionQueryResponse(TProjection projection)
    {
        Projection = projection;
    }

    public TProjection Projection { get; }
}