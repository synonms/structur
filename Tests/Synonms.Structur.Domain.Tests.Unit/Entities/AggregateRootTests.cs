using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Tests.Unit.Shared;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.Entities;

public class AggregateRootTests
{
    [Fact]
    public void Construction_WithIdAndAction_SetsIdAndActionAndNewVersionIsGenerated()
    {
        UserAction userAction = TestUser.UserAction;
        
        EntityId<TestAggregateRoot> id = EntityId<TestAggregateRoot>.New();
        TestAggregateRoot aggregateRoot = new(id, userAction, Moniker.Convert("name"), Units.Convert(1));
        
        Assert.Equal(id, aggregateRoot.Id);
        Assert.Equal(userAction.ActionAt, aggregateRoot.CreatedAction.ActionAt);
        Assert.Equal(userAction.ActionById, aggregateRoot.CreatedAction.ActionById);
        Assert.Equal(userAction.ActionByName, aggregateRoot.CreatedAction.ActionByName);
        Assert.False(aggregateRoot.EntityTag.IsEmpty);
        Assert.NotEqual(Guid.Empty, aggregateRoot.EntityTag.Value);
    }

    [Fact]
    public void UpdateProperty_DifferentValue_RecordsUpdatedActionAndUpdatesEntityTag()
    {
        const string originalValue = "original";
        const string updatedValue = "updated";

        UserAction userAction = TestUser.UserAction;

        EntityId<TestAggregateRoot> id = EntityId<TestAggregateRoot>.New();
        TestAggregateRoot aggregateRoot = new(id, userAction, Moniker.Convert(originalValue), Units.Convert(1));

        EntityTag originalEntityTag = aggregateRoot.EntityTag;
        DateTime originalCreatedAt = aggregateRoot.CreatedAction.ActionAt;
        Assert.Null(aggregateRoot.UpdatedAction);
        
        aggregateRoot.UpdateName(updatedValue, userAction);

        Assert.Equal(updatedValue, aggregateRoot.Name);
        Assert.NotEqual(originalEntityTag, aggregateRoot.EntityTag);
        Assert.Equal(originalCreatedAt, aggregateRoot.CreatedAction.ActionAt);
        Assert.NotNull(aggregateRoot.UpdatedAction);
    }
    
    [Fact]
    public void UpdateProperty_SameValue_IsNoOp()
    {
        const string originalValue = "original";
        
        UserAction userAction = TestUser.UserAction;

        EntityId<TestAggregateRoot> id = EntityId<TestAggregateRoot>.New();
        TestAggregateRoot aggregateRoot = new(id, userAction, Moniker.Convert(originalValue), Units.Convert(1));

        EntityTag originalEntityTag = aggregateRoot.EntityTag;
        DateTime originalCreatedAt = aggregateRoot.CreatedAction.ActionAt;
        Assert.Null(aggregateRoot.UpdatedAction);
        
        aggregateRoot.UpdateName(originalValue, userAction);

        Assert.Equal(originalValue, aggregateRoot.Name);
        Assert.Equal(originalEntityTag, aggregateRoot.EntityTag);
        Assert.Equal(originalCreatedAt, aggregateRoot.CreatedAction.ActionAt);
        Assert.Null(aggregateRoot.UpdatedAction);
    }
}