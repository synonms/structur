using System.Net;

namespace Synonms.Structur.Api.Client.Http;

public static class HttpResponseMessageExtensions
{
    public static Guid? ExtractResourceId(this HttpResponseMessage httpResponseMessage)
    {
        if (!httpResponseMessage.Headers.TryGetValues(nameof(HttpResponseHeader.Location), out IEnumerable<string>? locationHeaderValues))
        {
            return null;
        }

        if (!Guid.TryParse(new Uri(locationHeaderValues.First()).Segments[^1], out Guid id))
        {
            return null;
        }

        return id;
    }
    
    public static Guid? ExtractEntityTag(this HttpResponseMessage httpResponseMessage)
    {
        if (!httpResponseMessage.Headers.TryGetValues(nameof(HttpResponseHeader.ETag), out IEnumerable<string>? etagHeaderValues))
        {
            return null;
        }

        if (!Guid.TryParse(new Uri(etagHeaderValues.First()).Segments[^1], out Guid id))
        {
            return null;
        }

        return id;
    }
}