using Synonms.Structur.Domain.System;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.Tests.Unit.System;

public class TypeExtensionsTests
{
    [Fact]
    public void IsValueObject_GivenValueObject_ReturnsTrue()
    {
        Assert.True(typeof(UserAction).IsValueObject());
        
        Assert.True(typeof(BirthDate).IsValueObject());
        Assert.True(typeof(Currency).IsValueObject());
        Assert.True(typeof(EventDateTime).IsValueObject());
        Assert.True(typeof(Moniker).IsValueObject());
    }
}