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
        Assert.Equal(typeof(Address), typeof(Address).GetValueObjectValueType());
    }
    
    [Fact]
    public void IsValueObject_GivenValueObject_ReturnsTrue()
    {
        Assert.True(typeof(UserAction).IsValueObject());
        
        Assert.True(typeof(BirthDate).IsValueObject());
        Assert.True(typeof(Currency).IsValueObject());
        Assert.True(typeof(EventDateTime).IsValueObject());
        Assert.True(typeof(Moniker).IsValueObject());
        
        Assert.True(typeof(Address).IsValueObject());
    }
}