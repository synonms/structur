using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Domain.Tests.Unit.Shared;

internal class TestDomainEvent : DomainEvent<TestAggregateRoot>
{
    public TestDomainEvent(EntityId<TestAggregateRoot> aggregateId, Guid tenantId) : base(aggregateId, tenantId)
    {
    }

    public override Task<Result<TestAggregateRoot>> ApplyAsync(TestAggregateRoot? aggregateRoot)
    {
        if (aggregateRoot is null)
        {
            DomainEventFault fault = DomainEventFaults.CannotApplyToNull(nameof(TestDomainEvent), nameof(TestAggregateRoot));
            return Result<TestAggregateRoot>.FailureAsync(fault);
        }
        
        return Result<TestAggregateRoot>.SuccessAsync(aggregateRoot);
    }
    
    public override void Replay(Projection projection)
    {
    }
}

internal class SuccessfulTestDomainEventHandler : DomainEventHandler<TestDomainEvent>
{
    public int ExecutionCount { get; private set; } = 0;
    
    public override Task<Maybe<Fault>> HandleAsync(TestDomainEvent updatedEvent, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        
        return Maybe<Fault>.NoneAsync;
    }
}

internal class UnsuccessfulTestDomainEventHandler : DomainEventHandler<TestDomainEvent>
{
    public int ExecutionCount { get; private set; } = 0;

    public override Task<Maybe<Fault>> HandleAsync(TestDomainEvent updatedEvent, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;

        return Maybe<Fault>.SomeAsync(new Fault(nameof(UnsuccessfulTestDomainEventHandler), nameof(UnsuccessfulTestDomainEventHandler), nameof(UnsuccessfulTestDomainEventHandler), new FaultSource()));
    }
}