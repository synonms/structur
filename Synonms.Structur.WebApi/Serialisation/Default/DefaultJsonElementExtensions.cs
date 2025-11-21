using System.Text.Json;

namespace Synonms.Structur.WebApi.Serialisation.Default;

public static class DefaultJsonElementExtensions
{
    public static Uri GetDefaultUri(this JsonElement jsonElement)
    {
        string? href = jsonElement.TryGetProperty(DefaultPropertyNames.Links.Uri, out JsonElement hrefElement) ? hrefElement.GetString() : null;

        if (string.IsNullOrWhiteSpace(href))
        {
            throw new JsonException($"Unable to find [{DefaultPropertyNames.Links.Uri}] property.");
        }

        return new Uri(href, UriKind.RelativeOrAbsolute);
    }

    public static string GetDefaultLinkMethod(this JsonElement jsonElement) =>
        jsonElement.GetProperty(DefaultPropertyNames.Links.Method).GetString() ?? throw new JsonException($"Unable to extract [{DefaultPropertyNames.Links.Method}] property."); 

    public static string GetDefaultLinkRelation(this JsonElement jsonElement) =>
        jsonElement.GetProperty(DefaultPropertyNames.Links.Relation).GetString() ?? throw new JsonException($"Unable to extract [{DefaultPropertyNames.Links.Relation}] property."); 

    public static string[]? GetDefaultLinkAccepts(this JsonElement jsonElement) =>
        jsonElement.TryGetProperty(DefaultPropertyNames.Links.Accepts, out JsonElement methodElement) 
            ? JsonSerializer.Deserialize<string[]>(methodElement.ToString()) 
            : null;
}