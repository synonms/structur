using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Forms;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonFormDocumentJsonConverter : JsonConverter<FormDocument>
{
    public override FormDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument.");
        }

        using JsonDocument jsonDocument = doc;

        Uri uri = jsonDocument.RootElement.GetIonUri();
        string relation = jsonDocument.RootElement.GetIonLinkRelation();
        string method = jsonDocument.RootElement.GetIonLinkMethod();
        string[]? accepts = jsonDocument.RootElement.GetIonLinkAccepts();

        Link targetLink = new(uri, relation, method)
        {
            Accepts = accepts
        };
        
        JsonElement valueArray = jsonDocument.RootElement.GetProperty(IonPropertyNames.Value);

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
        
        JsonElement selfElement = jsonDocument.RootElement.GetProperty(IanaLinkRelationConstants.Self);
            
        Link? selfLink = JsonSerializer.Deserialize<Link>(selfElement.ToString(), options);

        if (selfLink is null)
        {
            throw new JsonException($"Unable to extract [{IanaLinkRelationConstants.Self}] link from document.");
        }

        FormDocument formDocument = new (selfLink, form);
        
        return formDocument;
    }

    public override void Write(Utf8JsonWriter writer, FormDocument value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        writer.WriteString(IonPropertyNames.Links.Uri, value.Form.Target.Uri.OriginalString);
        writer.WriteString(IonPropertyNames.Links.Relation, value.Form.Target.Relation);
        writer.WriteString(IonPropertyNames.Links.Method, value.Form.Target.Method);

        writer.WritePropertyName(IonPropertyNames.Value);
        JsonSerializer.Serialize(writer, (object)value.Form.Fields, options);
        
        foreach ((string key, Link link) in value.Links)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, (object)link, options);
        }

        writer.WriteEndObject();
    }
}