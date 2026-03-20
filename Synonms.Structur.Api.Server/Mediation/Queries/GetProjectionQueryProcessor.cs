using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class GetProjectionQueryProcessor<TAggregateRoot, TProjection> : IQueryHandler<GetProjectionQuery<TAggregateRoot, TProjection>, GetProjectionQueryResponse<TAggregateRoot, TProjection>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TProjection : Projection<TAggregateRoot>, new()
{
    private readonly IDomainEventRepository<TAggregateRoot> _domainEventRepository;

    public GetProjectionQueryProcessor(IDomainEventRepository<TAggregateRoot> domainEventRepository)
    {
        _domainEventRepository = domainEventRepository;
    }
    
    public async Task<Result<GetProjectionQueryResponse<TAggregateRoot, TProjection>>> HandleAsync(GetProjectionQuery<TAggregateRoot, TProjection> query, CancellationToken cancellationToken)
    {
        List<DomainEvent<TAggregateRoot>> domainEvents = (await _domainEventRepository.ReadAllAsync(query.Id, cancellationToken))
            .OrderBy(x => x.Timestamp)
            .ToList();

        if (domainEvents.Count == 0)
        {
            EntityNotFoundFault fault = new("Events for {0} with id '{1}' not found.", nameof(TAggregateRoot), query.Id);
            return Result<GetProjectionQueryResponse<TAggregateRoot, TProjection>>.Failure(fault);
        }
        
        TProjection projection = new();
        projection.Replay(query.Id, domainEvents);
        
        GetProjectionQueryResponse<TAggregateRoot, TProjection> response = new(projection);
        
        return response;
    }
}