using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Errors;

namespace Synonms.Structur.WebApi.Serialisation.Ion;

public class IonErrorCollectionDocumentJsonConverter : JsonConverter<ErrorCollectionDocument> 
{
    public override ErrorCollectionDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (!JsonDocument.TryParseValue(ref reader, out JsonDocument? doc))
        {
            throw new JsonException("Failed to parse JsonDocument");
        }

        using JsonDocument jsonDocument = doc;

        JsonElement errorsArray = jsonDocument.RootElement.GetProperty(IonPropertyNames.Errors);

        if (errorsArray.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException($"Expected [{IonPropertyNames.Errors}] property to be an array.");
        }

        List<Error> errors = new();
            
        foreach (JsonElement errorElement in errorsArray.EnumerateArray())
        {
            Error? error = JsonSerializer.Deserialize<Error>(errorElement.ToString(), options);

            if (error is null)
            {
                throw new JsonException("Unable to deserialise error.");
            }

            errors.Add(error);
        }

        JsonElement selfElement = jsonDocument.RootElement.GetProperty(IanaLinkRelationConstants.Self);
            
        Link? selfLink = JsonSerializer.Deserialize<Link>(selfElement.ToString(), options);

        if (selfLink is null)
        {
            throw new JsonException($"Unable to extract [{IanaLinkRelationConstants.Self}] link from document.");
        }

        ErrorCollectionDocument errorCollectionDocument = new(selfLink, errors);

        jsonDocument.RootElement.ForEachIonLinkProperty((linkName, link) => errorCollectionDocument.Links.Add(linkName, link), options);

        return errorCollectionDocument;
    }

    public override void Write(Utf8JsonWriter writer, ErrorCollectionDocument value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(IonPropertyNames.Errors);
        JsonSerializer.Serialize(writer, (object)value.Errors, options);

        foreach ((string key, Link link) in value.Links)
        {
            writer.WritePropertyName(key);
            JsonSerializer.Serialize(writer, (object)link, options);
        }

        writer.WriteEndObject();
    }
}