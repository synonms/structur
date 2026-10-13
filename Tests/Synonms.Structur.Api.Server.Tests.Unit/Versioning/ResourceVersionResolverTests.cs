using Synonms.Structur.Api.Server.Tests.Unit.Routing;
using Synonms.Structur.Api.Server.Versioning;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Versioning;

public class ResourceVersionResolverTests
{
    [Fact]
    public void Resolve_GivenUnspecifiedVersion_UsesUnspecifiedPropertyHistoryVersion()
    {
        Version? version = ResourceVersionResolver.Resolve(
            typeof(MultiRepresentationAggregate),
            typeof(MultiRepresentationPublicResource),
            null);

        Assert.Equal(new Version(), version);
    }

    [Fact]
    public void Resolve_GivenRegisteredExactVersion_ReturnsRequestedVersion()
    {
        Version? version = ResourceVersionResolver.Resolve(
            typeof(MultiRepresentationAggregate),
            typeof(MultiRepresentationPublicResource),
            new Version(1, 1));

        Assert.Equal(new Version(1, 1), version);
    }

    [Fact]
    public void Resolve_GivenUnregisteredVersion_ReturnsNoApplicableVersion()
    {
        Version? version = ResourceVersionResolver.Resolve(
            typeof(MultiRepresentationAggregate),
            typeof(MultiRepresentationPublicResource),
            new Version(2, 0));

        Assert.Null(version);
    }

    [Fact]
    public void Resolve_GivenNoRegistrationAllowList_PreservesRequestedVersion()
    {
        Version? version = ResourceVersionResolver.Resolve(
            typeof(MultiRepresentationAggregate),
            typeof(MultiRepresentationAdminResource),
            new Version(2, 0));

        Assert.Equal(new Version(2, 0), version);
    }
}
