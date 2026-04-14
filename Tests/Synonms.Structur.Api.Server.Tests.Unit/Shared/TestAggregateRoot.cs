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
}
