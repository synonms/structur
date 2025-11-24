using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Domain.Events;

public class WidgetDeletedEvent : AggregateDeletedDomainEvent<Widget>
{
    public WidgetDeletedEvent(EntityId<Widget> aggregateId) : base(aggregateId)
    {
    }

    public override void Replay(Projection projection)
    {
    }
}