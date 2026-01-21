using Synonms.Structur.Core.Faults;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.Faults;

public class FaultTests
{
    [Fact]
    public void ToString_GivenNoArgs_ReturnsOriginalString()
    {
        Fault fault = new("CODE", "Title", "Detail", new FaultSource());
        
        Assert.Equal("Detail", fault.ToString());
    }
    
    [Theory]
    [InlineData("plants", 99)]
    [InlineData("special sauce", 1991)]
    [InlineData("hickory_dickory", -12)]
    [InlineData("{Dennis}", 600)]
    public void ToString_GivenTemplateWithIndexedArgs_ReturnsFormattedString(string arg1, int arg2)
    {
        Fault fault = new("CODE", "Title", "Detail {0} {1}", new FaultSource(), arg1, arg2);
        
        Assert.Equal($"Detail {arg1} {arg2}", fault.ToString());
    }

    [Theory]
    [InlineData("plants", 99)]
    [InlineData("special sauce", 1991)]
    [InlineData("hickory_dickory", -12)]
    [InlineData("{Dennis}", 600)]
    public void ToString_GivenTemplateWithNamedArgs_ReturnsFormattedString(string arg1, int arg2)
    {
        Fault fault = new("CODE", "Title", "Detail {SomeText} {SomeNumber}", new FaultSource(), arg1, arg2);
        
        Assert.Equal($"Detail {arg1} {arg2}", fault.ToString());
    }

    [Fact]
    public void GetPlaceholders_GivenNoArgs_ReturnsEmptyList()
    {
        Fault fault = new("CODE", "Title", "Detail", new FaultSource());
        Assert.Empty(fault.GetPlaceholders());
    }
    
    [Fact]
    public void GetPlaceholders_GivenIndexedArgs_ReturnsList()
    {
        Fault fault = new("CODE", "Title", "Detail {0} and then {1} and maybe {2}", new FaultSource(), 122, "Macaroni", 2.34d);
        Assert.Collection(fault.GetPlaceholders(), 
            x =>
            {
                Assert.Equal("{0}", x.Key);
                Assert.Equal(122, x.Value);
            }, 
            x =>
            {
                Assert.Equal("{1}", x.Key);
                Assert.Equal("Macaroni", x.Value);
            }, 
            x =>
            {
                Assert.Equal("{2}", x.Key);
                Assert.Equal(2.34d, x.Value);
            } 
            );
    }

    [Fact]
    public void GetPlaceholders_GivenNamedArgs_ReturnsList()
    {
        Fault fault = new("CODE", "Title", "Detail {Name} and then {Age} and maybe {Height}", new FaultSource(), "Tony Macaroni", 74, 1.86d);
        Assert.Collection(fault.GetPlaceholders(),
            x =>
            {
                Assert.Equal("{Name}", x.Key);
                Assert.Equal("Tony Macaroni", x.Value);
            },
            x =>
            {
                Assert.Equal("{Age}", x.Key);
                Assert.Equal(74, x.Value);
            },
            x =>
            {
                Assert.Equal("{Height}", x.Key);
                Assert.Equal(1.86d, x.Value);
            }
        );
    }
}