using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualCreatedEvent : AggregateCreatedDomainEvent<Individual, IndividualResource>
{
    public IndividualCreatedEvent(EntityId<Individual> aggregateId, IndividualResource resource) : base(aggregateId, resource)
    {
    }
    
    public override Result<Individual> CreateAggregate(IndividualResource resource) => 
        Individual.Create(resource);

    public override void Replay(Projection projection)
    {
    }
}