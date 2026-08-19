using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Server.OpenApi.Default;

public static class DefaultOpenApiSchemaFactory
{
    public static IOpenApiSchema CreateForFormField() =>
        new OpenApiSchema()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>()
            {
                { DefaultPropertyNames.FormFields.Description, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.Enabled, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { DefaultPropertyNames.FormFields.ElementType, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.ElementForm, new OpenApiSchema() { Type = JsonSchemaType.Array } },
                { DefaultPropertyNames.FormFields.Form, new OpenApiSchema() { Type = JsonSchemaType.Array } },
                { DefaultPropertyNames.FormFields.Label, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.Max, new OpenApiSchema() },
                { DefaultPropertyNames.FormFields.MaxLength, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.MaxSize, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.Min, new OpenApiSchema() },
                { DefaultPropertyNames.FormFields.MinLength, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.MinSize, new OpenApiSchema() { Type = JsonSchemaType.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.Mutable, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { DefaultPropertyNames.FormFields.Name, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.Options, new OpenApiSchema() { Type = JsonSchemaType.Array } },
                { DefaultPropertyNames.FormFields.Pattern, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.Placeholder, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.Required, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { DefaultPropertyNames.FormFields.Secret, new OpenApiSchema() { Type = JsonSchemaType.Boolean } },
                { DefaultPropertyNames.FormFields.Type, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.FormFields.Value, new OpenApiSchema() },
                { DefaultPropertyNames.FormFields.Visible, new OpenApiSchema() { Type = JsonSchemaType.Boolean } }
            }                          
        };
    
    public static OpenApiSchema CreateForLink() =>
        new()
        {
            Type = JsonSchemaType.Object,
            Properties = new Dictionary<string, IOpenApiSchema>()
            {
                { DefaultPropertyNames.Links.Uri, new OpenApiSchema() { Type = JsonSchemaType.String, Format = "uri" } },
                { DefaultPropertyNames.Links.Relation, new OpenApiSchema() { Type = JsonSchemaType.String } },
                { DefaultPropertyNames.Links.Method, new OpenApiSchema() { Type = JsonSchemaType.String } }
            }                          
        };
    
    public static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute) =>
        OpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(logger, openApiDocument, resourceAttribute, resourceAttribute.ResourceType.Name + "_Default");
}