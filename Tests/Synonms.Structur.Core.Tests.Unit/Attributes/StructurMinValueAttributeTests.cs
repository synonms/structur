using Synonms.Structur.Core.Attributes;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.Attributes;

public class StructurMinValueAttributeTests
{
    [Fact]
    public void Constructor_Int_SetsProperties()
    {
        const int minimum = 10;
        StructurMinValueAttribute attribute = new(minimum);
        
        Assert.Equal(typeof(int), attribute.DataType);
        Assert.Equal(minimum, attribute.Minimum);
    }

    [Fact]
    public void Constructor_Long_SetsProperties()
    {
        const long minimum = 10;
        StructurMinValueAttribute attribute = new(minimum);
        
        Assert.Equal(typeof(long), attribute.DataType);
        Assert.Equal(minimum, attribute.Minimum);
    }

    [Fact]
    public void Constructor_Double_SetsProperties()
    {
        const double minimum = 10.0d;
        StructurMinValueAttribute attribute = new(minimum);
        
        Assert.Equal(typeof(double), attribute.DataType);
        Assert.Equal(minimum, attribute.Minimum);
    }

    [Fact]
    public void Constructor_Decimal_SetsProperties()
    {
        const decimal minimum = 10.0m;
        StructurMinValueAttribute attribute = new(minimum);
        
        Assert.Equal(typeof(decimal), attribute.DataType);
        Assert.Equal(minimum, attribute.Minimum);
    }
}