using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Http;

public static class HeaderDictionaryExtensions
{
    public static EntityTag? ExtractETag(this IHeaderDictionary headerDictionary) =>
        headerDictionary.ExtractEntityTag(HeaderNames.ETag);
    
    public static EntityTag? ExtractIfMatch(this IHeaderDictionary headerDictionary) =>
        headerDictionary.ExtractEntityTag(HeaderNames.IfMatch);

    public static EntityTag? ExtractIfNoneMatch(this IHeaderDictionary headerDictionary) =>
        headerDictionary.ExtractEntityTag(HeaderNames.IfNoneMatch);

    private static EntityTag? ExtractEntityTag(this IHeaderDictionary headerDictionary, string header)
    {
        if (headerDictionary.TryGetValue(header, out StringValues headerValues) is false)
        {
            return null;
        }

        if (headerValues.Count != 1)
        {
            return null;
        }

        string? entityTagRawValue = headerValues.Single();

        if (Guid.TryParse(entityTagRawValue, out Guid entityTagGuid) is false)
        {
            return null;
        }

        EntityTag entityTag = new(entityTagGuid);
            
        return entityTag;
    }
}