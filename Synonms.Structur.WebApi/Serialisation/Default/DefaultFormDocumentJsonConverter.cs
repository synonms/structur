using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Forms;

namespace Synonms.Structur.WebApi.Serialisation.Default;

public class DefaultFormDocumentJsonConverter : JsonConverter<FormDocument>
{
    public override FormDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument.");
        }

        using JsonDocument jsonDocument = doc;

        Uri uri = jsonDocument.RootElement.GetDefaultUri();
        string relation = jsonDocument.RootElement.GetDefaultLinkRelation();
        string method = jsonDocument.RootElement.GetDefaultLinkMethod();
        string[]? accepts = jsonDocument.RootElement.GetDefaultLinkAccepts();

        Link targetLink = new(uri, relation, method)
        {
            Accepts = accepts
        };
        
        JsonElement valueArray = jsonDocument.RootElement.GetProperty(DefaultPropertyNames.Value);

        IEnumerable<FormField>? formFields = JsonSerializer.Deserialize<IEnumerable<FormField>>(valueArray.ToString(), options);

        if (formFields is null)
        {
            throw new JsonException("Failed to extract form fields from document.");
        }
        
        Form? form = new (targetLink, formFields);

        if (form is null)
        {
            throw new JsonException("Unable to extract form from document.");
        }
        
        string selfLinkJson = jsonDocument.RootElement.TryGetProperty(IanaLinkRelationConstants.Self, out JsonElement selfElement) ? selfElement.ToString() : string.Empty;
            
        Link selfLink = JsonSerializer.Deserialize<Link>(selfLinkJson, options) ?? Link.EmptyLink();

        FormDocument formDocument = new (selfLink, form);
        
        return formDocument;
    }

    public override void Write(Utf8JsonWriter writer, FormDocument value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString(DefaultPropertyNames.Links.Uri, value.Form.Target.Uri.OriginalString);
        writer.WriteString(DefaultPropertyNames.Links.Relation, value.Form.Target.Relation);
        writer.WriteString(DefaultPropertyNames.Links.Method, value.Form.Target.Method);

        writer.WritePropertyName(DefaultPropertyNames.Value);
        JsonSerializer.Serialize(writer, (object)value.Form.Fields, options);
        
        if (value.Links.TryGetValue(IanaLinkRelationConstants.Self, out Link? selfLink))
        {
            writer.WritePropertyName(IanaLinkRelationConstants.Self);
            JsonSerializer.Serialize(writer, (object)selfLink, options);
        }

        writer.WriteEndObject();
    }
}