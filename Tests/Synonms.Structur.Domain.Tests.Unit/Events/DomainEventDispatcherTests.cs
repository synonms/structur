using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Tests.Unit.Shared;

namespace Synonms.Structur.Domain.Tests.Unit.Events;

public class DomainEventDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_GivenNoHandlers_ReturnsNoFault()
    {
        DomainEventDispatcher dispatcher = new([]);
        
        Maybe<Fault> outcome = await dispatcher.DispatchAsync(new TestDomainEvent(EntityId<TestAggregateRoot>.New(), Guid.NewGuid()));
        
        Assert.True(outcome.IsNone);
    }
    
    [Fact]
    public async Task DispatchAsync_GivenSingleSuccessfulHandler_ReturnsNoFault()
    {
        SuccessfulTestDomainEventHandler successfulHandler = new();
        DomainEventDispatcher dispatcher = new([successfulHandler]);
        
        Maybe<Fault> outcome = await dispatcher.DispatchAsync(new TestDomainEvent(EntityId<TestAggregateRoot>.New(), Guid.NewGuid()));
        
        Assert.True(outcome.IsNone);
        Assert.Equal(1, successfulHandler.ExecutionCount);
    }
    
    [Fact]
    public async Task DispatchAsync_GivenMultipleSuccessfulHandlers_ReturnsNoFault()
    {
        SuccessfulTestDomainEventHandler successfulHandler1 = new();
        SuccessfulTestDomainEventHandler successfulHandler2 = new();
        DomainEventDispatcher dispatcher = new([successfulHandler1, successfulHandler2]);
        
        Maybe<Fault> outcome = await dispatcher.DispatchAsync(new TestDomainEvent(EntityId<TestAggregateRoot>.New(), Guid.NewGuid()));
        
        Assert.True(outcome.IsNone);
        Assert.Equal(1, successfulHandler1.ExecutionCount);
        Assert.Equal(1, successfulHandler2.ExecutionCount);
    }
    
    [Fact]
    public async Task DispatchAsync_GivenMultipleMixedHandlers_ReturnsFault()
    {
        SuccessfulTestDomainEventHandler successfulHandler1 = new();
        SuccessfulTestDomainEventHandler successfulHandler2 = new();
        UnsuccessfulTestDomainEventHandler unsuccessfulHandler1 = new();
        UnsuccessfulTestDomainEventHandler unsuccessfulHandler2 = new();
        DomainEventDispatcher dispatcher = new([successfulHandler1, successfulHandler2, unsuccessfulHandler1, unsuccessfulHandler2]);
        
        Maybe<Fault> outcome = await dispatcher.DispatchAsync(new TestDomainEvent(EntityId<TestAggregateRoot>.New(), Guid.NewGuid()));
        
        Assert.True(outcome.IsSome);
        Assert.Equal(1, successfulHandler1.ExecutionCount);
        Assert.Equal(1, successfulHandler2.ExecutionCount);
        Assert.Equal(1, unsuccessfulHandler1.ExecutionCount);
        Assert.Equal(1, unsuccessfulHandler2.ExecutionCount);
    }
    
    [Fact]
    public async Task DispatchAsync_GivenSingleUnsuccessfulHandler_ReturnsFault()
    {
        UnsuccessfulTestDomainEventHandler unsuccessfulHandler = new();
        DomainEventDispatcher dispatcher = new([unsuccessfulHandler]);
        
        Maybe<Fault> outcome = await dispatcher.DispatchAsync(new TestDomainEvent(EntityId<TestAggregateRoot>.New(), Guid.NewGuid()));
        
        Assert.True(outcome.IsSome);
        Assert.Equal(1, unsuccessfulHandler.ExecutionCount);
    }
    
    [Fact]
    public async Task DispatchAsync_GivenMultipleUnsuccessfulHandlers_ReturnsFault()
    {
        UnsuccessfulTestDomainEventHandler unsuccessfulHandler1 = new();
        UnsuccessfulTestDomainEventHandler unsuccessfulHandler2 = new();
        DomainEventDispatcher dispatcher = new([unsuccessfulHandler1, unsuccessfulHandler2]);
        
        Maybe<Fault> outcome = await dispatcher.DispatchAsync(new TestDomainEvent(EntityId<TestAggregateRoot>.New(), Guid.NewGuid()));
        
        Assert.True(outcome.IsSome);
        Assert.Equal(1, unsuccessfulHandler1.ExecutionCount);
        Assert.Equal(1, unsuccessfulHandler2.ExecutionCount);
    }
}