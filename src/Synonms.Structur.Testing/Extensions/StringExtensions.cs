namespace Synonms.Structur.Testing.Extensions;

public static class StringExtensions
{
    public static Uri ToAbsoluteUri(this string uriString) => new (uriString, UriKind.Absolute);
    
    public static Uri ToRelativeUri(this string uriString) => new (uriString, UriKind.Relative);
}