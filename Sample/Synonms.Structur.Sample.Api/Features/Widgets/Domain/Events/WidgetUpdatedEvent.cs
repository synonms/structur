using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Widgets.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Domain.Events;

public class WidgetUpdatedEvent : AggregateUpdatedDomainEvent<Widget, WidgetResource>
{
    public WidgetUpdatedEvent(EntityId<Widget> aggregateId, WidgetResource resource) : base(aggregateId, resource)
    {
    }

    public override Maybe<Fault> UpdateAggregate(Widget aggregateRoot, WidgetResource resource) => 
        aggregateRoot.Update(resource);

    public override void Replay(Projection projection)
    {
    }
}
