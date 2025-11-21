using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.WebApi.Domain;

public interface IDomainEventFactory<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    DomainEvent<TAggregateRoot> GenerateCreatedEvent(TResource resource);
    
    DomainEvent<TAggregateRoot> GenerateDeletedEvent(EntityId<TAggregateRoot> aggregateId);

    DomainEvent<TAggregateRoot> GenerateUpdatedEvent(TResource resource);
}