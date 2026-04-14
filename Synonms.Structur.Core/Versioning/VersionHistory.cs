namespace Synonms.Structur.Core.Versioning;

public class VersionHistory
{
    public VersionHistory(Version introduced, Version? deprecated = null)
    {
        Introduced = introduced;
        Deprecated = deprecated;
    }

    public Version Introduced { get; }
    public Version? Deprecated { get; }

    public bool IsDeprecated => Deprecated is not null && !Deprecated.IsUnspecified();

    public bool IsApplicableAtVersion(Version version)
    {
        if (version.IsUnspecified()) return !IsDeprecated;
        
        return (Introduced.IsUnspecified() || version >= Introduced) && (!IsDeprecated || version < Deprecated);
    }

    /// <summary>
    /// Used when there are Versions applicable at multiple levels i.e. Class and Property.
    /// The Primary version values take precedence if specified, otherwise the Secondary values are used.
    /// Generally the more specific version (e.g. the Property) should be used as the Primary and more general (e.g. Class) used as the Secondary.
    /// </summary>
    /// <param name="primary">The main versions to use if specified.</param>
    /// <param name="secondary">The versions to fall back to if the primary versions are unspecified.</param>
    /// <returns></returns>
    public static VersionHistory Merge(VersionHistory primary, VersionHistory secondary) =>
        new VersionHistory(
            primary.Introduced.IsUnspecified() ? secondary.Introduced : primary.Introduced,
            primary.IsDeprecated ? primary.Deprecated : secondary.Deprecated);
}