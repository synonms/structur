using Synonms.Structur.Core.System;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.System;

public class StringExtensionsTests
{
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ToCamelCase_EmptyOrWhitespace_ReturnsEmptyString(string input)
    {
        string output = input.ToCamelCase();
        
        Assert.Equal(string.Empty, output);
    }

    [Theory]
    [InlineData("testValue")]
    [InlineData("TestValue")]
    [InlineData(" TestValue")]
    [InlineData("TestValue ")]
    [InlineData("Test Value")]
    [InlineData("test value")]
    public void ToCamelCase_Populated_ReturnsCamelCase(string input)
    {
        string output = input.ToCamelCase();
        
        Assert.Equal("testValue", output);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ToPascalCase_EmptyOrWhitespace_ReturnsEmptyString(string input)
    {
        string output = input.ToPascalCase();
        
        Assert.Equal(string.Empty, output);
    }

    [Theory]
    [InlineData("testValue")]
    [InlineData("TestValue")]
    [InlineData(" TestValue")]
    [InlineData("TestValue ")]
    [InlineData("Test Value")]
    [InlineData("test value")]
    public void ToPascalCase_Populated_ReturnsCamelCase(string input)
    {
        string output = input.ToPascalCase();
        
        Assert.Equal("TestValue", output);
    }
    
    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-99")]
    [InlineData("-2147483648")]
    public void TryParsePositiveInt_NegativeOrZeroInt_ReturnsFalse(string value)
    {
        Assert.False(value.TryParsePositiveInt(out int _));
    }
    
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("1.5")]
    [InlineData("-99999999999")]
    [InlineData("-2147483649")]
    [InlineData("2147483648")]
    [InlineData("99999999999")]
    [InlineData("i am not an int")]
    public void TryParsePositiveInt_NotAnInt_ReturnsFalse(string value)
    {
        Assert.False(value.TryParsePositiveInt(out int _));
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("99", 99)]
    [InlineData("2147483647", int.MaxValue)]
    public void TryParsePositiveInt_PositiveInt_ReturnsTrue(string value, int expectedResult)
    {
        Assert.True(value.TryParsePositiveInt(out int actualResult));
        Assert.Equal(expectedResult, actualResult);
    }
}