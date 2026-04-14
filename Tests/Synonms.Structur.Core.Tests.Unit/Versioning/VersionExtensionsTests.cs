using Synonms.Structur.Core.Versioning;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.Versioning;

public class VersionExtensionsTests
{
    [Fact]
    public void IsUnspecified_GivenDefault_ReturnsTrue()
    {
        Version version = new();
        Assert.True(version.IsUnspecified());
    }
    
    [Fact]
    public void IsUnspecified_GivenZeros_ReturnsTrue()
    {
        Version version = new(0, 0);
        Assert.True(version.IsUnspecified());
    }
}