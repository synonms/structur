using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Events;

public interface IDomainEventFactory<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    Result<DomainEvent<TAggregateRoot>> GenerateCreatedEvent(TResource resource);
    
    Result<DomainEvent<TAggregateRoot>> GenerateDeletedEvent(EntityId<TAggregateRoot> aggregateId);

    Result<DomainEvent<TAggregateRoot>> GenerateUpdatedEvent(TResource resource);
}