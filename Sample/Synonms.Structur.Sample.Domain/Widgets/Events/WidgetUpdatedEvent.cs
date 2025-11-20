using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Sample.Domain.Widgets.Events;

public class WidgetUpdatedEvent : DomainEvent<Widget, WidgetUpdateRequest>
{
    public WidgetUpdatedEvent(EntityId<Widget> aggregateId, WidgetUpdateRequest trigger) : base(aggregateId, trigger)
    {
    }

    public override Task<Result<Widget>> ApplyAsync(Widget? aggregateRoot)
    {
        if (aggregateRoot is null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNull(nameof(WidgetUpdatedEvent), nameof(Widget));
            return Result<Widget>.Failure(fault).AsAsync();
        }

        return aggregateRoot.Update(Trigger).ToResult(() => aggregateRoot).AsAsync();
    }

    public override void Replay(Projection projection)
    {
    }
}


public class WidgetUpdateRequest : EventTrigger
{
    public string Name { get; init; } = string.Empty;
}