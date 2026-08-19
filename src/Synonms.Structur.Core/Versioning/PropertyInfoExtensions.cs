using System.Reflection;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Core.Versioning;

public static class PropertyInfoExtensions
{
    public static VersionHistory GetVersionHistory(this PropertyInfo propertyInfo)
    {
        Version introducedVersion = new();
        Version? deprecatedVersion = null;

        propertyInfo.ApplyAttribute<StructurVersionHistoryAttribute>(attribute =>
        {
            introducedVersion = new Version(attribute.IntroducedMajorVersion, attribute.IntroducedMinorVersion);
            deprecatedVersion = new Version(attribute.DeprecatedMajorVersion, attribute.DeprecatedMinorVersion);
        });

        return new VersionHistory(introducedVersion, deprecatedVersion);
    }
    
    private static void ApplyAttribute<TAttribute>(this PropertyInfo propertyInfo, Action<TAttribute> action)
    {
        if (propertyInfo.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() is TAttribute attribute)
        {
            action(attribute);
        }
    }
}