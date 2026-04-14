using Synonms.Structur.Core.Versioning;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.Versioning;

public class VersionHistoryTests
{
    [Fact]
    public void IsDeprecated_GivenNullDeprecated_ReturnsFalse()
    {
        VersionHistory versionHistory = new(new Version(1, 0), null);
        
        Assert.False(versionHistory.IsDeprecated);
    }

    [Fact]
    public void IsDeprecated_GivenDefaultDeprecated_ReturnsFalse()
    {
        VersionHistory versionHistory = new(new Version(1, 0), new Version());
        
        Assert.False(versionHistory.IsDeprecated);
    }

    [Fact]
    public void IsDeprecated_GivenZeroDeprecated_ReturnsFalse()
    {
        VersionHistory versionHistory = new(new Version(1, 0), new Version(0, 0));
        
        Assert.False(versionHistory.IsDeprecated);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void IsApplicableAtVersion_GivenDefaultIntroducedAndNullDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(), null);
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void IsApplicableAtVersion_GivenZeroIntroducedAndNullDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(0, 0), null);
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void IsApplicableAtVersion_GivenDefaultIntroducedAndDefaultDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(), new Version());
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void IsApplicableAtVersion_GivenZeroIntroducedAndDefaultDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(0, 0), new Version());
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }
    
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void IsApplicableAtVersion_GivenDefaultIntroducedAndZeroDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(), new Version(0, 0));
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    public void IsApplicableAtVersion_GivenZeroIntroducedAndZeroDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(0, 0), new Version(0, 0));
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }
    
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 0)]
    [InlineData(9, 8)]
    [InlineData(9, 9)]
    public void IsApplicableAtVersion_GivenAfterIntroducedAndNotDeprecated_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(1, 0), null);
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }
    
    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(5, 0)]
    [InlineData(9, 8)]
    public void IsApplicableAtVersion_GivenWithinRange_ReturnsTrue(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(1, 0), new Version(9, 9));
        
        Assert.True(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(3, 9)]
    public void IsApplicableAtVersion_GivenBeforeStart_ReturnsFalse(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(4, 0), null);
        
        Assert.False(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Theory]
    [InlineData(2, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 9)]
    public void IsApplicableAtVersion_GivenAfterEnd_ReturnsFalse(int major, int minor)
    {
        VersionHistory versionHistory = new(new Version(1, 0), new Version(2, 0));
        
        Assert.False(versionHistory.IsApplicableAtVersion(new Version(major, minor)));
    }

    [Fact]
    public void IsApplicableAtVersion_GivenDefaultVersionAndNotDeprecated_ReturnsTrue()
    {
        VersionHistory unspecifiedNull = new(new Version(), null);
        Assert.True(unspecifiedNull.IsApplicableAtVersion(new Version()));
        
        VersionHistory unspecifiedUnspecified = new(new Version(), new Version());
        Assert.True(unspecifiedUnspecified.IsApplicableAtVersion(new Version()));

        VersionHistory unspecifiedZero = new(new Version(), new Version(0, 0));
        Assert.True(unspecifiedZero.IsApplicableAtVersion(new Version()));
    }

    [Fact]
    public void IsApplicableAtVersion_GivenZeroVersionAndNotDeprecated_ReturnsTrue()
    {
        VersionHistory unspecifiedNull = new(new Version(), null);
        Assert.True(unspecifiedNull.IsApplicableAtVersion(new Version(0, 0)));
        
        VersionHistory unspecifiedUnspecified = new(new Version(), new Version());
        Assert.True(unspecifiedUnspecified.IsApplicableAtVersion(new Version(0, 0)));

        VersionHistory unspecifiedZero = new(new Version(), new Version(0, 0));
        Assert.True(unspecifiedZero.IsApplicableAtVersion(new Version(0, 0)));
    }

    [Fact]
    public void IsApplicableAtVersion_GivenDefaultVersionAndDeprecated_ReturnsFalse()
    {
        VersionHistory unspecifiedStart = new(new Version(), new Version(1, 0));
        Assert.False(unspecifiedStart.IsApplicableAtVersion(new Version()));
        
        VersionHistory specifiedStart = new(new Version(1, 0), new Version(1, 1));
        Assert.False(specifiedStart.IsApplicableAtVersion(new Version()));
    }

    [Fact]
    public void IsApplicableAtVersion_GivenZeroVersionAndDeprecated_ReturnsFalse()
    {
        VersionHistory unspecifiedStart = new(new Version(), new Version(1, 0));
        Assert.False(unspecifiedStart.IsApplicableAtVersion(new Version(0, 0)));
        
        VersionHistory specifiedStart = new(new Version(1, 0), new Version(1, 1));
        Assert.False(specifiedStart.IsApplicableAtVersion(new Version(0, 0)));
    }
}