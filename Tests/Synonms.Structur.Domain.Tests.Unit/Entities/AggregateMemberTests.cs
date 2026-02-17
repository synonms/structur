using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Tests.Unit.Shared;

namespace Synonms.Structur.Domain.Tests.Unit.Entities;

public class AggregateMemberTests
{
    [Fact]
    public void Construction_WithId_SetsId()
    {
        EntityId<TestAggregateMember> id = EntityId<TestAggregateMember>.New();
        TestAggregateMember aggregateMember = new(id);
        
        Assert.Equal(id, aggregateMember.Id);
    }
}