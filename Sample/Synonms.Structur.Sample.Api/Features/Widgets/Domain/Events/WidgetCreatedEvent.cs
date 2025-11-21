using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Widgets.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Domain.Events;

public class WidgetCreatedEvent : AggregateCreatedDomainEvent<Widget, WidgetResource>
{
    public WidgetCreatedEvent(EntityId<Widget> aggregateId, WidgetResource resource) : base(aggregateId, resource)
    {
    }

    public override Result<Widget> CreateAggregate(WidgetResource trigger) => 
        Widget.Create(trigger);

    public override void Replay(Projection projection)
    {
    }
}
