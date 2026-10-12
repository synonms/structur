using Synonms.Structur.Domain.System;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.System;

public class TypeExtensionsTests
{
    [Fact]
    public void GetValueObjectValueType_GivenValueObject_ReturnsCorrectValueType()
    {
        Assert.Equal(typeof(string), typeof(AddressType).GetValueObjectValueType());
        Assert.Equal(typeof(DateOnly), typeof(BirthDate).GetValueObjectValueType());
        Assert.Equal(typeof(DateTime), typeof(EventDateTime).GetValueObjectValueType());
        Assert.Equal(typeof(int), typeof(Units).GetValueObjectValueType());
    }

    [Fact]
    public void GetValueObjectValueType_GivenComplexValueObject_ReturnsNull()
    {
        Assert.Null(typeof(Address).GetValueObjectValueType());
        Assert.Null(typeof(EmailContact).GetValueObjectValueType());
        Assert.Null(typeof(TelephoneContact).GetValueObjectValueType());
    }

    [Fact]
    public void IsValueObject_GivenValueObject_ReturnsTrue()
    {
        Assert.True(typeof(BirthDate).IsValueObject());
        Assert.True(typeof(Currency).IsValueObject());
        Assert.True(typeof(EventDateTime).IsValueObject());
        Assert.True(typeof(Moniker).IsValueObject());
    }
    
    [Fact]
    public void IsValueObject_GivenComplexValueObject_ReturnsFalse()
    {
        Assert.False(typeof(UserAction).IsValueObject());
        Assert.False(typeof(Address).IsValueObject());
    }
}