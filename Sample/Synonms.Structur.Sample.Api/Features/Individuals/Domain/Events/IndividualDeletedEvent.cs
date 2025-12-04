using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualDeletedEvent : AggregateDeletedDomainEvent<Individual>
{
    public IndividualDeletedEvent(EntityId<Individual> aggregateId, Guid tenantId) : base(aggregateId, tenantId)
    {
    }

    public override void Replay(Projection projection)
    {
    }
}