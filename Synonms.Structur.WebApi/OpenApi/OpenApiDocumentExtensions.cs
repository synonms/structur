using Microsoft.OpenApi.Models;

namespace Synonms.Structur.WebApi.OpenApi;

public static class OpenApiDocumentExtensions
{
    public static OpenApiSchema GetOrCreateSchemaReference(this OpenApiDocument openApiDocument, string componentSchemaName, Func<OpenApiSchema> schemaFunc)
    {
        if (openApiDocument.Components is null)
        {
            openApiDocument.Components = new OpenApiComponents();
        }

        if (openApiDocument.Components.Schemas is null)
        {
            openApiDocument.Components.Schemas = new Dictionary<string, OpenApiSchema>();
        }
        
        if (openApiDocument.Components.Schemas.ContainsKey(componentSchemaName) is false)
        {
            OpenApiSchema componentSchema = schemaFunc.Invoke();

            openApiDocument.Components.Schemas.Add(componentSchemaName, componentSchema);
        }
        
        OpenApiSchema schemaWithReference = new()
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.Schema,
                Id = componentSchemaName
            }
        };
        
        return schemaWithReference;
    }
}