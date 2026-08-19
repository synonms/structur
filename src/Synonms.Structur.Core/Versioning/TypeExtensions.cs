using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Core.Versioning;

public static class TypeExtensions
{
    public static VersionHistory GetVersionHistory(this Type type)
    {
        Version introducedVersion = new();
        Version? deprecatedVersion = null;

        type.ApplyAttribute<StructurVersionHistoryAttribute>(attribute =>
        {
            introducedVersion = new Version(attribute.IntroducedMajorVersion, attribute.IntroducedMinorVersion);
            deprecatedVersion = new Version(attribute.DeprecatedMajorVersion, attribute.DeprecatedMinorVersion);
        });

        return new VersionHistory(introducedVersion, deprecatedVersion);
    }
    
    private static TAttribute? GetAttribute<TAttribute>(this Type type) where TAttribute : Attribute
    {
        return type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() as TAttribute;
    }

    private static void ApplyAttribute<TAttribute>(this Type type, Action<TAttribute> action) where TAttribute : Attribute
    {
        if (type.GetCustomAttributes(typeof(TAttribute), true).FirstOrDefault() is TAttribute attribute)
        {
            action(attribute);
        }
    }
}