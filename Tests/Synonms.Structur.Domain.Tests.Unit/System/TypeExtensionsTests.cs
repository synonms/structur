using Synonms.Structur.Domain.System;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.System;

public class TypeExtensionsTests
{
    [Fact]
    public void GetSimpleValueObjectValueType_GivenSimpleValueObject_ReturnsCorrectValueType()
    {
        Assert.Equal(typeof(string), typeof(AddressType).GetSimpleValueObjectValueType());
        Assert.Equal(typeof(DateOnly), typeof(BirthDate).GetSimpleValueObjectValueType());
        Assert.Equal(typeof(DateTime), typeof(EventDateTime).GetSimpleValueObjectValueType());
        Assert.Equal(typeof(int), typeof(Units).GetSimpleValueObjectValueType());
    }

    [Fact]
    public void GetSimpleValueObjectValueType_GivenComplexValueObject_ReturnsNull()
    {
        Assert.Null(typeof(Address).GetSimpleValueObjectValueType());
        Assert.Null(typeof(EmailContact).GetSimpleValueObjectValueType());
        Assert.Null(typeof(TelephoneContact).GetSimpleValueObjectValueType());
    }

    [Fact]
    public void IsSimpleValueObject_GivenSimpleValueObject_ReturnsTrue()
    {
        Assert.True(typeof(BirthDate).IsSimpleValueObject());
        Assert.True(typeof(Currency).IsSimpleValueObject());
        Assert.True(typeof(EventDateTime).IsSimpleValueObject());
        Assert.True(typeof(Moniker).IsSimpleValueObject());
    }
    
    [Fact]
    public void IsSimpleValueObject_GivenComplexValueObject_ReturnsFalse()
    {
        Assert.False(typeof(UserAction).IsSimpleValueObject());
        Assert.False(typeof(Address).IsSimpleValueObject());
    }
}