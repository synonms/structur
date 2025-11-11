using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Domain.Widgets.Events;

namespace Synonms.Structur.Sample.Domain.Widgets;

public class Widget : AggregateRoot<Widget>
{
    private Widget() : base(GuidEntityId<Widget>.Uninitialised)
    {
    }
    
    private Widget(GuidEntityId<Widget> id, Moniker name) : base(id)
    {
    }

    public Moniker Name { get; private set; } = null!;
    
    internal Maybe<Fault> Update(WidgetUpdatedTrigger trigger) =>
        Entity.CreateBuilder<Widget>()
            .WithMandatoryValueObject(trigger.Name, x => Moniker.CreateMandatory(nameof(Name), x), out Moniker nameValueObject)
            .Build()
            .BiBind(() =>
                {
                    Name = nameValueObject;

                    return Maybe<Fault>.None;
                });

    internal static Result<Widget> Create(WidgetCreatedTrigger trigger) =>
        Entity.CreateBuilder<Widget>()
            .WithMandatoryValueObject(trigger.Name, x => Moniker.CreateMandatory(nameof(Name), x), out Moniker nameValueObject)
            .Build()
            .ToResult(() => new Widget(GuidEntityId<Widget>.New(trigger.Id), nameValueObject));
}