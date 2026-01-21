using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.Shared;

internal class TestAggregateMember : AggregateMember<TestAggregateMember>
{
    private TestAggregateMember()
    {
    }

    public TestAggregateMember(EntityId<TestAggregateMember> id, UserAction createdAction) : base(id, createdAction)
    {
    }
}