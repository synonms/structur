using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Tests.Unit.Shared;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.Validation;

public class EntityBuilderTests
{
    [Fact]
    public void WithOptionalValueObject_GivenNullValueType_ReturnsNoFaultAndOutputsNull()
    {
        int? value = null;

        Maybe<Fault> outcome = Entity.CreateBuilder<TestAggregateRoot>()
            .WithOptionalValueObject(value, x => Units.CreateOptional(nameof(TestAggregateRoot.Units), x), out Units? unitsValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.Null(unitsValueObject);
    }
    
    [Fact]
    public void WithOptionalValueObject_GivenNullReferenceType_ReturnsNoFaultAndOutputsNull()
    {
        string? value = null;

        Maybe<Fault> outcome = Entity.CreateBuilder<TestAggregateRoot>()
            .WithOptionalValueObject(value, x => Moniker.CreateOptional(nameof(TestAggregateRoot.Name), x), out Moniker? nameValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.Null(nameValueObject);
    }
    
    [Fact]
    public void WithOptionalValueObject_GivenValueType_ReturnsNoFaultAndOutputsValueObject()
    {
        int value = 99;

        Maybe<Fault> outcome = Entity.CreateBuilder<TestAggregateRoot>()
            .WithOptionalValueObject(value, x => Units.CreateOptional(nameof(TestAggregateRoot.Units), x), out Units? unitsValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.NotNull(unitsValueObject);
        Assert.Equal(value, unitsValueObject.Value);
    }
    
    [Fact]
    public void WithOptionalValueObject_GivenReferenceType_ReturnsNoFaultAndOutputsValueObject()
    {
        string value = "Pizzeria";

        Maybe<Fault> outcome = Entity.CreateBuilder<TestAggregateRoot>()
            .WithOptionalValueObject(value, x => Moniker.CreateOptional(nameof(TestAggregateRoot.Name), x), out Moniker? nameValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.NotNull(nameValueObject);
        Assert.Equal(value, nameValueObject.Value);
    }
    
    [Fact]
    public void WithOptionalValueObject_GivenInvalidValueType_ReturnsFaultAndOutputsNull()
    {
        int value = -99;

        Maybe<Fault> outcome = Entity.CreateBuilder<TestAggregateRoot>()
            .WithOptionalValueObject(value, x => Units.CreateOptional(nameof(TestAggregateRoot.Units), x, 0, 100), out Units? unitsValueObject)
            .Build();

        Assert.True(outcome.IsSome);
        Assert.Null(unitsValueObject);
    }
    
    [Fact]
    public void WithOptionalValueObject_GivenInvalidReferenceType_ReturnsFaultAndOutputsNull()
    {
        string value = "SUPERDUPERLONGNAME";

        Maybe<Fault> outcome = Entity.CreateBuilder<TestAggregateRoot>()
            .WithOptionalValueObject(value, x => Moniker.CreateOptional(nameof(TestAggregateRoot.Name), x, maxLength: 4), out Moniker? nameValueObject)
            .Build();

        Assert.True(outcome.IsSome);
        Assert.Null(nameValueObject);
    }
}