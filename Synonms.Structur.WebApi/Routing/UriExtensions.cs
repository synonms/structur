namespace Synonms.Structur.WebApi.Routing;

public static class UriExtensions
{
    public static string ToRelativePath(this Uri uri)
    {
        return uri.IsAbsoluteUri ? uri.PathAndQuery : uri.OriginalString;
    }

    public static Uri ToRelativeUri(this Uri uri)
    {
        string path = uri.IsAbsoluteUri ? uri.PathAndQuery : uri.OriginalString;
        
        return new Uri(path, UriKind.Relative);
    }

    public static string ToAbsolutePath(this Uri uri, string baseUrl)
    {
        Uri baseUri = new(baseUrl);

        return uri.ToAbsolutePath(baseUri);
    }

    public static string ToAbsolutePath(this Uri uri, Uri baseUri)
    {
        string relativePath = uri.ToRelativePath();

        if (Uri.TryCreate(baseUri, relativePath, out Uri? absoluteUri))
        {
            return absoluteUri.ToString();
        }

        return uri.IsAbsoluteUri ? uri.ToString() : string.Empty;
    }
    
    public static Uri ToAbsoluteUri(this Uri uri, string baseUrl)
    {
        Uri baseUri = new(baseUrl);

        return uri.ToAbsoluteUri(baseUri);
    }
    
    public static Uri ToAbsoluteUri(this Uri uri, Uri baseUri)
    {
        string relativePath = uri.ToRelativePath();

        if (Uri.TryCreate(baseUri, relativePath, out Uri? absoluteUri))
        {
            return absoluteUri;
        }

        return uri.IsAbsoluteUri ? uri : baseUri;
    }
}