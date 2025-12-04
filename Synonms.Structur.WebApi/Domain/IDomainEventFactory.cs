using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.WebApi.Domain;

public interface IDomainEventFactory<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    Result<DomainEvent<TAggregateRoot>> GenerateCreatedEvent(TResource resource);
    
    Result<DomainEvent<TAggregateRoot>> GenerateDeletedEvent(EntityId<TAggregateRoot> aggregateId);

    Result<DomainEvent<TAggregateRoot>> GenerateUpdatedEvent(TResource resource);
}