using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Schema;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonLinkJsonConverter : JsonConverter<Link>
{
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsAssignableTo(typeof(Link));

    public override Link Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        Uri uri = jsonDocument.RootElement.GetIonUri();
        string relation = jsonDocument.RootElement.GetIonLinkRelation();
        string method = jsonDocument.RootElement.GetIonLinkMethod();
        string[]? accepts = jsonDocument.RootElement.GetIonLinkAccepts();

        Link link = new(uri, relation, method)
        {
            Accepts = accepts
        };
            
        return link;
    }

    public override void Write(Utf8JsonWriter writer, Link value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteString(IonPropertyNames.Links.Uri, value.Uri.OriginalString);
        writer.WriteString(IonPropertyNames.Links.Relation, value.Relation);
        writer.WriteString(IonPropertyNames.Links.Method, value.Method);

        if (value.Accepts?.Any() ?? false)
        {
            writer.WritePropertyName(IonPropertyNames.Links.Accepts);
            JsonSerializer.Serialize(writer, value.Accepts, options);
        }

        writer.WriteEndObject();
    }
}