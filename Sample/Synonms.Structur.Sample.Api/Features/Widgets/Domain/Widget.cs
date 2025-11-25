using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Widgets.Presentation;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Domain;

[StructurResource(typeof(WidgetResource), "widgets", allowAnonymous: true, pageLimit: 5)]
public class Widget : AggregateRoot<Widget>
{
    public const int NameMaxLength = 250;
    
    private Widget() : base(EntityId<Widget>.Uninitialised)
    {
    }
    
    private Widget(EntityId<Widget> id, Moniker name) : base(id)
    {
        Name = name;
    }

    public Moniker Name { get; private set; } = null!;
    
    internal Maybe<Fault> Update(WidgetResource resource) =>
        Entity.CreateBuilder<Widget>()
            .WithMandatoryValueObject(resource.Name, x => Moniker.CreateMandatory(nameof(Name), x), out Moniker nameValueObject)
            .Build()
            .BiBind(() =>
                {
                    Name = nameValueObject;

                    return Maybe<Fault>.None;
                });

    internal static Result<Widget> Create(WidgetResource resource) =>
        Entity.CreateBuilder<Widget>()
            .WithMandatoryValueObject(resource.Name, x => Moniker.CreateMandatory(nameof(Name), x, NameMaxLength), out Moniker nameValueObject)
            .Build()
            .ToResult(() => new Widget((EntityId<Widget>)resource.Id, nameValueObject));
}