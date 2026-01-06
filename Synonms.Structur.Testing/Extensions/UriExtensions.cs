namespace Synonms.Structur.Testing.Extensions;

public static class UriExtensions
{
    public static Guid? ExtractTrailingId(this Uri? uri)
    {
        string? uriString = uri?.ToString();
        
        if (string.IsNullOrWhiteSpace(uriString))
        {
            return null;
        }
        
        int lastPartIndex = uriString.LastIndexOf('/');

        if (lastPartIndex <= 0 || lastPartIndex >= uriString.Length - 1)
        {
            return null;
        }

        string lastPart = uriString[(lastPartIndex + 1)..];

        return Guid.TryParse(lastPart, out Guid id) ? id : null;
    }
}