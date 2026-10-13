using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Serialisation;

namespace Synonms.Structur.Api.Server.Hypermedia;

internal static class ResourceContractInputReader
{
    internal const string MetadataItemKey = "Synonms.Structur.ResourceRequestMetadata";

    internal static object? Deserialize(string body, Type contractType, JsonSerializerOptions options, HttpContext httpContext)
    {
        object? resource = ResourceJsonDataReader.Deserialize(body, contractType, options, out ResourceMetadata metadata);
        httpContext.Items[MetadataItemKey] = metadata;
        return resource;
    }

    internal static ResourceMetadata GetMetadata(HttpContext httpContext, Guid? id = null)
    {
        if (id is not null)
        {
            if (httpContext.Items.TryGetValue(MetadataItemKey, out object? existingValue)
                && existingValue is ResourceMetadata existingMetadata)
            {
                existingMetadata.Id = id.Value;
                return existingMetadata;
            }

            ResourceMetadata routeMetadata = new(id.Value, Link.EmptyLink());
            httpContext.Items[MetadataItemKey] = routeMetadata;
            return routeMetadata;
        }

        if (httpContext.Items.TryGetValue(MetadataItemKey, out object? value) && value is ResourceMetadata metadata)
        {
            return metadata;
        }

        ResourceMetadata generatedMetadata = new(Guid.NewGuid(), Link.EmptyLink());
        httpContext.Items[MetadataItemKey] = generatedMetadata;
        return generatedMetadata;
    }
}
