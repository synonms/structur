using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.WebApi.Serialisation.Default;

namespace Synonms.Structur.WebApi.OpenApi.Default;

public static class DefaultOpenApiSchemaFactory
{
    public static OpenApiSchema CreateForFormField() =>
        new()
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>()
            {
                { DefaultPropertyNames.FormFields.Description, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.Enabled, new OpenApiSchema() { Type = OpenApiDataTypes.Boolean } },
                { DefaultPropertyNames.FormFields.ElementType, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.ElementForm, new OpenApiSchema() { Type = OpenApiDataTypes.Array } },
                { DefaultPropertyNames.FormFields.Form, new OpenApiSchema() { Type = OpenApiDataTypes.Array } },
                { DefaultPropertyNames.FormFields.Label, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.Max, new OpenApiSchema() },
                { DefaultPropertyNames.FormFields.MaxLength, new OpenApiSchema() { Type = OpenApiDataTypes.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.MaxSize, new OpenApiSchema() { Type = OpenApiDataTypes.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.Min, new OpenApiSchema() },
                { DefaultPropertyNames.FormFields.MinLength, new OpenApiSchema() { Type = OpenApiDataTypes.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.MinSize, new OpenApiSchema() { Type = OpenApiDataTypes.Integer, Format = OpenApiIntegerFormats.Int32 } },
                { DefaultPropertyNames.FormFields.Mutable, new OpenApiSchema() { Type = OpenApiDataTypes.Boolean } },
                { DefaultPropertyNames.FormFields.Name, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.Options, new OpenApiSchema() { Type = OpenApiDataTypes.Array } },
                { DefaultPropertyNames.FormFields.Pattern, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.Placeholder, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.Required, new OpenApiSchema() { Type = OpenApiDataTypes.Boolean } },
                { DefaultPropertyNames.FormFields.Secret, new OpenApiSchema() { Type = OpenApiDataTypes.Boolean } },
                { DefaultPropertyNames.FormFields.Type, new OpenApiSchema() { Type = OpenApiDataTypes.String } },
                { DefaultPropertyNames.FormFields.Value, new OpenApiSchema() },
                { DefaultPropertyNames.FormFields.Visible, new OpenApiSchema() { Type = OpenApiDataTypes.Boolean } }
            }                          
        };
    
    public static OpenApiSchema CreateForLink() =>
        new()
        {
            Type = "object",
            Properties = new Dictionary<string, OpenApiSchema>()
            {
                { DefaultPropertyNames.Links.Uri, new OpenApiSchema() { Type = "string", Format = "uri" } },
                { DefaultPropertyNames.Links.Relation, new OpenApiSchema() { Type = "string" } },
                { DefaultPropertyNames.Links.Method, new OpenApiSchema() { Type = "string" } }
            }                          
        };
    
    public static OpenApiSchema GetOrCreateSchemaReferenceForResource(ILogger logger, OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute) =>
        OpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(logger, openApiDocument, resourceAttribute, resourceAttribute.ResourceType.Name + "_Default");
}