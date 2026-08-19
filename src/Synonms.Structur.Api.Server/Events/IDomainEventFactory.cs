using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Events;

public interface IDomainEventFactory<TAggregateRoot, in TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    Task<Result<DomainEvent<TAggregateRoot>>> GenerateCreatedEvent(TResource resource, CancellationToken cancellationToken = default);
    
    Task<Result<DomainEvent<TAggregateRoot>>> GenerateDeletedEvent(EntityId<TAggregateRoot> aggregateId, CancellationToken cancellationToken = default);

    Task<Result<DomainEvent<TAggregateRoot>>> GenerateUpdatedEvent(TResource resource, CancellationToken cancellationToken = default);
}