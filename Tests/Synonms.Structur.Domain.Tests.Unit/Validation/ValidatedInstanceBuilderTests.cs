using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Tests.Unit.Shared;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.Validation;

public class ValidatedInstanceBuilderTests
{
    [Fact]
    public void WithOptionalScalarProperty_GivenNullValueType_ReturnsNoFaultAndOutputsNull()
    {
        int? value = null;

        Maybe<DomainRulesFault> outcome = Validator.CreateBuilder<TestAggregateRoot>()
            .WithOptionalScalarProperty(value, x => Units.CreateOptional(nameof(TestAggregateRoot.Units), x), out Units? unitsValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.Null(unitsValueObject);
    }
    
    [Fact]
    public void WithOptionalScalarProperty_GivenNullReferenceType_ReturnsNoFaultAndOutputsNull()
    {
        string? value = null;

        Maybe<DomainRulesFault> outcome = Validator.CreateBuilder<TestAggregateRoot>()
            .WithOptionalScalarProperty(value, x => Moniker.CreateOptional(nameof(TestAggregateRoot.Name), x), out Moniker? nameValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.Null(nameValueObject);
    }
    
    [Fact]
    public void WithOptionalScalarProperty_GivenValueType_ReturnsNoFaultAndOutputsValueObject()
    {
        int value = 99;

        Maybe<DomainRulesFault> outcome = Validator.CreateBuilder<TestAggregateRoot>()
            .WithOptionalScalarProperty(value, x => Units.CreateOptional(nameof(TestAggregateRoot.Units), x), out Units? unitsValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.NotNull(unitsValueObject);
        Assert.Equal(value, unitsValueObject.Value);
    }
    
    [Fact]
    public void WithOptionalScalarProperty_GivenReferenceType_ReturnsNoFaultAndOutputsValueObject()
    {
        string value = "Pizzeria";

        Maybe<DomainRulesFault> outcome = Validator.CreateBuilder<TestAggregateRoot>()
            .WithOptionalScalarProperty(value, x => Moniker.CreateOptional(nameof(TestAggregateRoot.Name), x), out Moniker? nameValueObject)
            .Build();

        Assert.True(outcome.IsNone);
        Assert.NotNull(nameValueObject);
        Assert.Equal(value, nameValueObject.Value);
    }
    
    [Fact]
    public void WithOptionalScalarProperty_GivenInvalidValueType_ReturnsFaultAndOutputsNull()
    {
        int value = -99;

        Maybe<DomainRulesFault> outcome = Validator.CreateBuilder<TestAggregateRoot>()
            .WithOptionalScalarProperty(value, x => Units.CreateOptional(nameof(TestAggregateRoot.Units), x, 0, 100), out Units? unitsValueObject)
            .Build();

        Assert.True(outcome.IsSome);
        Assert.Null(unitsValueObject);
    }
    
    [Fact]
    public void WithOptionalScalarProperty_GivenInvalidReferenceType_ReturnsFaultAndOutputsNull()
    {
        string value = "SUPERDUPERLONGNAME";

        Maybe<DomainRulesFault> outcome = Validator.CreateBuilder<TestAggregateRoot>()
            .WithOptionalScalarProperty(value, x => Moniker.CreateOptional(nameof(TestAggregateRoot.Name), x, maxLength: 4), out Moniker? nameValueObject)
            .Build();

        Assert.True(outcome.IsSome);
        Assert.Null(nameValueObject);
    }
}