using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualUpdatedEvent: AggregateUpdatedDomainEvent<Individual, IndividualResource>
{
    public IndividualUpdatedEvent(EntityId<Individual> aggregateId, IndividualResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
    }

    public override Maybe<Fault> UpdateAggregate(Individual aggregateRoot, IndividualResource resource) =>
        aggregateRoot.Update(resource);

    public override void Replay(Projection projection)
    {
    }
}