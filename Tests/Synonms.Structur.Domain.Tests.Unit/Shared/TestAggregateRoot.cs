using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.Shared;

internal class TestAggregateRoot : AggregateRoot<TestAggregateRoot>
{
    private TestAggregateRoot()
    {
    }
        
    public TestAggregateRoot(EntityId<TestAggregateRoot> id, UserAction createdAction, Moniker name, Units units) : base(id, createdAction)
    {
        Name = name;
        Units = units;
    }

    public Moniker Name { get; set; } = null!;

    public Units Units { get; set; } = null!;

    public void UpdateName(string name, UserAction updatedAction)
    {
        Moniker nameValueObject = Moniker.Convert(name);
        
        UpdateMandatoryValue(x => x.Name, nameValueObject, updatedAction);
    }
}
