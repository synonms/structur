using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Application.Tests.Unit.Shared;

internal class TestAggregateRoot : AggregateRoot<TestAggregateRoot>
{
    private TestAggregateRoot()
    {
    }
        
    public TestAggregateRoot(EntityId<TestAggregateRoot> id, UserAction createdAction) : base(id, createdAction)
    {
    }
}
