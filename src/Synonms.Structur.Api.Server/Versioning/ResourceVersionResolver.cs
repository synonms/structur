using System.Reflection;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Versioning;

namespace Synonms.Structur.Api.Server.Versioning;

public static class ResourceVersionResolver
{
    public static Version? Resolve(Type aggregateRootType, Type resourceType, Version? requestedVersion)
    {
        StructurResourceAttribute registration = aggregateRootType
            .GetCustomAttributes<StructurResourceAttribute>()
            .SingleOrDefault(attribute => attribute.ResourceType == resourceType)
            ?? throw new InvalidOperationException(
                $"No {nameof(StructurResourceAttribute)} registration exists for {aggregateRootType.FullName} -> {resourceType.FullName}.");

        if (requestedVersion is null || requestedVersion.IsUnspecified())
        {
            return new Version();
        }

        if (registration.SupportedApiVersions.Length == 0)
        {
            return requestedVersion;
        }

        Version[] supportedVersions = registration.SupportedApiVersions
            .Select(version => Version.Parse(version))
            .ToArray();

        return supportedVersions.Contains(requestedVersion) ? requestedVersion : null;
    }
}
