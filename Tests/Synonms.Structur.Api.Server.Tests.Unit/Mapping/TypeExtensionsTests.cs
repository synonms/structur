using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Domain.ValueObjects;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Mapping;

public class TypeExtensionsTests
{
    [Fact]
    public void GetValueObjectValue_GivenValueObject_ReturnsValue()
    {
        // Arrange
        const string expectedValue = "Test Value";
        Moniker valueObject = Moniker.Rehydrate(expectedValue);

        // Act
        object? value = valueObject.GetType().GetValueObjectValue(valueObject);

        // Assert
        Assert.Equal(expectedValue, value);
    }
}