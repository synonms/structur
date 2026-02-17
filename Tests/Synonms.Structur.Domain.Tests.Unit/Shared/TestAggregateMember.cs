using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Domain.Tests.Unit.Shared;

internal class TestAggregateMember : AggregateMember<TestAggregateMember>
{
    private TestAggregateMember()
    {
    }

    public TestAggregateMember(EntityId<TestAggregateMember> id) : base(id)
    {
    }
}