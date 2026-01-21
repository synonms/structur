using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Tests.Unit.Shared;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.Entities;

public class AggregateMemberTests
{
    [Fact]
    public void Construction_WithIdAndAction_SetsIdAndAction()
    {
        UserAction userAction = TestUser.UserAction;
        
        EntityId<TestAggregateMember> id = EntityId<TestAggregateMember>.New();
        TestAggregateMember aggregateMember = new(id, userAction);
        
        Assert.Equal(id, aggregateMember.Id);
        Assert.Equal(userAction.ActionAt, aggregateMember.CreatedAction.ActionAt);
        Assert.Equal(userAction.ActionById, aggregateMember.CreatedAction.ActionById);
        Assert.Equal(userAction.ActionByName, aggregateMember.CreatedAction.ActionByName);
    }
}