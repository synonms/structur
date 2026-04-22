using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Synonms.Structur.Api.Core.Serialisation.Ion;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Server.OpenApi.Ion;

public static class IonOpenApiSchemaFactory
{
    public static IOpenApiSchema CreateForFormField() =>
        new OpenApiSchema()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>()
            {
                { IonPropertyNames.FormFields.Description, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.Enabled, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { IonPropertyNames.FormFields.ElementType, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.ElementForm, new OpenApiSchema() { Type = JsonSchemaType.Array } },
                { IonPropertyNames.FormFields.Form, new OpenApiSchema() { Type = JsonSchemaType.Array } },
                { IonPropertyNames.FormFields.Label, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.Max, new OpenApiSchema() },
                { IonPropertyNames.FormFields.MaxLength, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { IonPropertyNames.FormFields.MaxSize, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { IonPropertyNames.FormFields.Min, new OpenApiSchema() },
                { IonPropertyNames.FormFields.MinLength, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { IonPropertyNames.FormFields.MinSize, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { IonPropertyNames.FormFields.Mutable, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { IonPropertyNames.FormFields.Name, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.Options, new OpenApiSchema() { Type = JsonSchemaType.Array } },
                { IonPropertyNames.FormFields.Pattern, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.Placeholder, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.Required, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { IonPropertyNames.FormFields.Secret, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { IonPropertyNames.FormFields.Type, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.FormFields.Value, new OpenApiSchema() },
                { IonPropertyNames.FormFields.Visible, new OpenApiSchema() { Type = JsonSchemaType.Boolean } }
            }                          
        };
    
    public static OpenApiSchema CreateForLink() =>
        new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>()
            {
                { IonPropertyNames.Links.Uri, new OpenApiSchema() { Type = JsonSchemaType.String, Format = "uri" } },
                { IonPropertyNames.Links.Relation, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { IonPropertyNames.Links.Method, new OpenApiSchema() { Type = JsonSchemaType.String } }
            }
        };
    
    public static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute)
    {
        Dictionary<string, IOpenApiSchema> additionalProperties = new()
        {
            { "self", CreateForLink() }
        };

        if (resourceAttribute.IsUpdateDisabled is false)
        {
            additionalProperties.Add("edit-form", CreateForLink());
        }

        if (resourceAttribute.IsDeleteDisabled is false)
        {
            additionalProperties.Add("delete", CreateForLink());
        }
        
        OpenApiSchema schema = OpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(logger, openApiDocument, resourceAttribute, resourceAttribute.ResourceType.Name + "_Ion", additionalProperties);
        
        return schema;
    }
}