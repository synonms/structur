using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Api.Server.Tests.Unit.Shared;

public class TestAggregateRoot : AggregateRoot<TestAggregateRoot>
{
    private TestAggregateRoot()
    {
    }

    public TestAggregateRoot(EntityId<TestAggregateRoot> id, UserAction createdAction) : base(id, createdAction)
    {
    }

    public EntityId<TestAggregateRoot> SomeRelatedAggregateId { get; set; } = null!;

    public Moniker SomeString { get; set; } = null!;

    public Units SomeInt { get; set; } = null!;

    public bool SomeBool { get; set; }

    public List<Moniker> SomeList { get; set; } = null!;
    
    public EventDate SomeDate { get; set; } = null!;
    
    public EventDateTime SomeDateTime { get; set; } = null!;

    public TestAggregateMember ChildResource { get; set; } = null!;
    
    public List<TestAggregateMember> ChildResources { get; set; } = [];

    public static TestAggregateRoot Create() =>
        new(EntityId<TestAggregateRoot>.New(), TestUser.UserAction)
        {
            SomeRelatedAggregateId = EntityId<TestAggregateRoot>.New(),
            SomeString = Moniker.Convert("Some string"),
            SomeInt = Units.Convert(123),
            SomeBool = true,
            SomeList = [ Moniker.Convert("Some array item 1"), Moniker.Convert("Some array item 2") ],
            SomeDate = EventDate.Convert(new DateOnly(2024, 1, 1)),
            SomeDateTime = EventDateTime.Convert(new DateTime(2024, 1, 1, 12, 0, 0)),
            ChildResource = TestAggregateMember.Create(),
            ChildResources = [TestAggregateMember.Create(), TestAggregateMember.Create()]
        };
}

public class TestAggregateMember : AggregateMember<TestAggregateMember>
{
    private TestAggregateMember()
    {
    }
        
    public TestAggregateMember(EntityId<TestAggregateMember> id) : base(id)
    {
    }

    public Moniker Property1 { get; set; } = null!;

    public Units Property2 { get; set; } = null!;

    public static TestAggregateMember Create() =>
        new(EntityId<TestAggregateMember>.New())
        {
            Property1 = Moniker.Convert("Some string"),
            Property2 = Units.Convert(123)
        };
}