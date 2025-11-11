using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Sample.Domain.Widgets.Events;

public class WidgetCreatedEvent : DomainEvent<Widget, WidgetCreatedTrigger>
{
    public WidgetCreatedEvent(EntityId<Widget> aggregateId, WidgetCreatedTrigger trigger) : base(aggregateId, trigger)
    {
    }

    public override Task<Result<Widget>> ApplyAsync(Widget? aggregateRoot)
    {
        if (aggregateRoot is not null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNonNull(nameof(WidgetCreatedEvent), nameof(Widget));
            return Result<Widget>.Failure(fault).AsAsync();
        }

        return Widget.Create(Trigger).AsAsync();
    }

    public override void Replay(Projection projection)
    {
    }
}

public class WidgetCreatedTrigger : EventTrigger
{
    public Guid Id { get; init; } = Guid.NewGuid();
    
    public string Name { get; init; } = string.Empty;
}