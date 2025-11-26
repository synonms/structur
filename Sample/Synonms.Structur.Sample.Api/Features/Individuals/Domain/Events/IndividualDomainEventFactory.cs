using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualDomainEventFactory : IDomainEventFactory<Individual, IndividualResource>
{
    public DomainEvent<Individual> GenerateCreatedEvent(IndividualResource resource) =>
        new IndividualCreatedEvent((EntityId<Individual>)resource.Id, resource);

    public DomainEvent<Individual> GenerateDeletedEvent(EntityId<Individual> aggregateId) =>
        new IndividualDeletedEvent(aggregateId);

    public DomainEvent<Individual> GenerateUpdatedEvent(IndividualResource resource) =>
        new IndividualUpdatedEvent((EntityId<Individual>)Guid.NewGuid(), resource);
}