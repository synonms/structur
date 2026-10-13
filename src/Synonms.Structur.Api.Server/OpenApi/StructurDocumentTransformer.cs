using System.Reflection;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Api.Core.Serialisation.Ion;
using Synonms.Structur.Api.Server.OpenApi.Default;
using Synonms.Structur.Api.Server.OpenApi.Ion;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.Api.Server.OpenApi;

public class StructurDocumentTransformer : IOpenApiDocumentTransformer
{
    private readonly ILogger<StructurDocumentTransformer> _logger;
    private readonly IResourceDirectory _resourceDirectory;

    public StructurDocumentTransformer(ILogger<StructurDocumentTransformer> logger, IResourceDirectory resourceDirectory)
    {
        _logger = logger;
        _resourceDirectory = resourceDirectory;
    }

    public Task TransformAsync(OpenApiDocument openApiDocument, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        foreach (IResourceDirectory.AggregateRootLayout aggregateRootLayout in _resourceDirectory.GetAllRoots())
        {
            string collectionName = aggregateRootLayout.CollectionPath;
            StructurResourceAttribute resourceAttribute = aggregateRootLayout.Registration;

            Dictionary<HttpMethod, OpenApiOperation> collectionOperations = [];
            if (!resourceAttribute.IsReadDisabled)
            {
                collectionOperations.Add(
                    HttpMethod.Get,
                    AddSupportedVersions(GetAllOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout));
            }

            if (resourceAttribute.IsCreateDisabled is false)
            {
                OpenApiOperation postOperation = AddSupportedVersions(PostOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout);
                collectionOperations.Add(HttpMethod.Post, postOperation);

                if (!resourceAttribute.IsCreateFormDisabled)
                {
                    OpenApiOperation createFormOperation = AddSupportedVersions(CreateFormOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout);
                    openApiDocument.Paths.Add("/" + collectionName + "/create-form", new OpenApiPathItem
                    {
                        Operations = new Dictionary<HttpMethod, OpenApiOperation>
                        {
                            [HttpMethod.Get] = createFormOperation
                        }
                    });
                }
            }

            if (collectionOperations.Count > 0)
            {
                openApiDocument.Paths.Add("/" + collectionName, new OpenApiPathItem
                {
                    Operations = collectionOperations
                });
            }

            Dictionary<HttpMethod, OpenApiOperation> resourceOperations = [];
            if (!resourceAttribute.IsReadDisabled)
            {
                resourceOperations.Add(
                    HttpMethod.Get,
                    AddSupportedVersions(GetByIdOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout));
            }

            if (resourceAttribute.IsUpdateDisabled is false)
            {
                OpenApiOperation putOperation = AddSupportedVersions(PutOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout);
                resourceOperations.Add(HttpMethod.Put, putOperation);

                if (!resourceAttribute.IsEditFormDisabled)
                {
                    OpenApiOperation editFormOperation = AddSupportedVersions(EditFormOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout);
                    openApiDocument.Paths.Add("/" + collectionName + "/{id}/edit-form", new OpenApiPathItem
                    {
                        Operations = new Dictionary<HttpMethod, OpenApiOperation>
                        {
                            [HttpMethod.Get] = editFormOperation
                        }
                    });
                }
            }

            if (resourceAttribute.IsDeleteDisabled is false)
            {
                OpenApiOperation deleteOperation = AddSupportedVersions(DeleteOperation(openApiDocument, collectionName, resourceAttribute), aggregateRootLayout);
                resourceOperations.Add(HttpMethod.Delete, deleteOperation);
            }

            if (resourceOperations.Count > 0)
            {
                openApiDocument.Paths.Add("/" + collectionName + "/{id}", new OpenApiPathItem
                {
                    Operations = resourceOperations
                });
            }
        }

        return Task.CompletedTask;
    }

    private static OpenApiOperation AddSupportedVersions(OpenApiOperation operation, IResourceDirectory.AggregateRootLayout layout)
    {
        if (layout.SupportedApiVersions.Count == 0)
        {
            return operation;
        }

        List<JsonNode> versions = layout.SupportedApiVersions
            .Order()
            .Select(JsonNode (version) => JsonValue.Create(version.ToString())!)
            .ToList();

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HttpHeaders.ApiVersion,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Requested API version. This representation supports: " + string.Join(", ", layout.SupportedApiVersions.Order()),
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Enum = versions }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HttpHeaders.Version,
            In = ParameterLocation.Header,
            Required = false,
            Description = "Legacy version header. If supplied, it must match a supported API version.",
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = layout.SupportedApiVersions.Order().Select(JsonNode (version) => JsonValue.Create(version.ToString())!).ToList()
            }
        });
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = HttpQueryStringKeys.ApiVersion,
            In = ParameterLocation.Query,
            Required = false,
            Description = "Requested API version. This representation supports: " + string.Join(", ", layout.SupportedApiVersions.Order()),
            Schema = new OpenApiSchema
            {
                Type = JsonSchemaType.String,
                Enum = layout.SupportedApiVersions.Order().Select(JsonNode (version) => JsonValue.Create(version.ToString())!).ToList()
            }
        });
        operation.Responses ??= new OpenApiResponses();
        operation.Responses.TryAdd("400", new OpenApiResponse
        {
            Description = "The requested API version is not supported by this resource representation."
        });

        return operation;
    }

    private OpenApiOperation CreateFormOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation createFormOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".CreateForm",
            Summary = "Get a form describing how to add a new resource to a collection.",
            Tags = GetTagsForCollection(openApiDocument, collectionName)
        };

        Dictionary<string, IOpenApiSchema> defaultCreateFormProperties = new()
        {
            {
                DefaultPropertyNames.Value, new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = DefaultOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, defaultCreateFormProperties, "Default");

        Dictionary<string, IOpenApiSchema> ionCreateFormProperties = new()
        {
            { IonPropertyNames.Links.Uri, new OpenApiSchema() { Type = JsonSchemaType.String, Format = "uri" } },
            { IonPropertyNames.Links.Relation, new OpenApiSchema() { Type = JsonSchemaType.String } },
            { IonPropertyNames.Links.Method, new OpenApiSchema() { Type = JsonSchemaType.String } },
            {
                IonPropertyNames.Value, new OpenApiSchema()
                {
                    Type = JsonSchemaType.Array,
                    Items = IonOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, ionCreateFormProperties, "Ion");

        if (createFormOperation.Responses is null)
        {
            createFormOperation.Responses = new OpenApiResponses();
        }

        createFormOperation.Responses.Add("200", new OpenApiResponse()
        {
            Description = "Success",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypes.Json] = new() { Schema = defaultSchema },
                [MediaTypes.Ion] = new() { Schema = ionSchema }
            }
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (createFormOperation.Security is null)
            {
                createFormOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            createFormOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return createFormOperation;
    }

    private OpenApiOperation DeleteOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation deleteOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".Delete",
            Summary = "Deletes an existing resource.",
            Tags = GetTagsForCollection(openApiDocument, collectionName),
            Parameters = new List<IOpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid"
                    }
                }
            }
        };

        if (deleteOperation.Responses is null)
        {
            deleteOperation.Responses = new OpenApiResponses();
        }

        deleteOperation.Responses.Add("204", new OpenApiResponse
        {
            Description = "Successfully deleted"
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (deleteOperation.Security is null)
            {
                deleteOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            deleteOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return deleteOperation;
    }

    private OpenApiOperation EditFormOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation editFormOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".EditForm",
            Summary = "Get a form describing how to update an existing resource.",
            Tags = GetTagsForCollection(openApiDocument, collectionName),
            Parameters = new List<IOpenApiParameter>
            {
                new OpenApiParameter
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid"
                    }
                }
            }
        };

        Dictionary<string, IOpenApiSchema> defaultEditFormProperties = new()
        {
            {
                DefaultPropertyNames.Value, new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = DefaultOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, defaultEditFormProperties, "Default");

        Dictionary<string, IOpenApiSchema> ionCreateFormProperties = new()
        {
            { IonPropertyNames.Links.Uri, new OpenApiSchema() { Type = JsonSchemaType.String, Format = "uri" } },
            { IonPropertyNames.Links.Relation, new OpenApiSchema() { Type = JsonSchemaType.String } },
            { IonPropertyNames.Links.Method, new OpenApiSchema() { Type = JsonSchemaType.String } },
            {
                IonPropertyNames.Value, new OpenApiSchema()
                {
                    Type = JsonSchemaType.Array,
                    Items = IonOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, ionCreateFormProperties, "Ion");

        if (editFormOperation.Responses is null)
        {
            editFormOperation.Responses = new OpenApiResponses();
        }

        editFormOperation.Responses.Add("200", new OpenApiResponse
        {
            Description = "Success",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypes.Json] = new() { Schema = defaultSchema },
                [MediaTypes.Ion] = new() { Schema = ionSchema }
            }
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (editFormOperation.Security is null)
            {
                editFormOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            editFormOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return editFormOperation;
    }

    private OpenApiOperation GetAllOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation getAllOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".GetAll",
            Summary = "Get a paged collection of resources.",
            Tags = GetTagsForCollection(openApiDocument, collectionName)
        };

        Dictionary<string, IOpenApiSchema> defaultResourceCollectionDocumentProperties = new()
        {
            {
                DefaultPropertyNames.Value, new OpenApiSchema
                {
                    Type = JsonSchemaType.Array,
                    Items = DefaultOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute)
                }
            },
            { DefaultPropertyNames.Pagination.Offset, new OpenApiSchema() { Type = JsonSchemaType.Integer } },
            { DefaultPropertyNames.Pagination.Limit, new OpenApiSchema() { Type = JsonSchemaType.Integer } },
            { DefaultPropertyNames.Pagination.Size, new OpenApiSchema() { Type = JsonSchemaType.Integer } }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, defaultResourceCollectionDocumentProperties, "Default", true);

        Dictionary<string, IOpenApiSchema> ionResourceCollectionDocumentProperties = new()
        {
            {
                IonPropertyNames.Value, new OpenApiSchema()
                {
                    Type = JsonSchemaType.Array,
                    Items = IonOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute)
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() },
            { "first", IonOpenApiSchemaFactory.CreateForLink() },
            { "previous", IonOpenApiSchemaFactory.CreateForLink() },
            { "next", IonOpenApiSchemaFactory.CreateForLink() },
            { "last", IonOpenApiSchemaFactory.CreateForLink() },
            { "offset", new OpenApiSchema() { Type = JsonSchemaType.Integer } },
            { "limit", new OpenApiSchema() { Type = JsonSchemaType.Integer } },
            { "size", new OpenApiSchema() { Type = JsonSchemaType.Integer } }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, ionResourceCollectionDocumentProperties, "Ion", true);

        if (resourceAttribute.IsCreateDisabled is false)
        {
            ionResourceCollectionDocumentProperties.Add("create-form", IonOpenApiSchemaFactory.CreateForLink());
        }

        if (getAllOperation.Responses is null)
        {
            getAllOperation.Responses = new OpenApiResponses();
        }

        getAllOperation.Responses.Add("200", new OpenApiResponse
        {
            Description = "Success",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypes.Json] = new() { Schema = defaultSchema },
                [MediaTypes.Ion] = new() { Schema = ionSchema }
            }
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (getAllOperation.Security is null)
            {
                getAllOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            getAllOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return getAllOperation;
    }

    private OpenApiOperation GetByIdOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation getByIdOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".GetById",
            Summary = "Get an individual resource by Id.",
            Tags = GetTagsForCollection(openApiDocument, collectionName),
            Parameters = new List<IOpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid"
                    }
                }
            }
        };

        Dictionary<string, IOpenApiSchema> defaultResourceDocumentProperties = new()
        {
            { DefaultPropertyNames.Value, DefaultOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute) },
            { IanaLinkRelationConstants.Self, DefaultOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, defaultResourceDocumentProperties, "Default");

        Dictionary<string, IOpenApiSchema> ionResourceDocumentProperties = new()
        {
            { IonPropertyNames.Value, IonOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute) },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, ionResourceDocumentProperties, "Ion");

        if (getByIdOperation.Responses is null)
        {
            getByIdOperation.Responses = new OpenApiResponses();
        }

        getByIdOperation.Responses.Add("200", new OpenApiResponse()
        {
            Description = "Success",
            Content = new Dictionary<string, OpenApiMediaType>
            {
                [MediaTypes.Json] = new() { Schema = defaultSchema },
                [MediaTypes.Ion] = new() { Schema = ionSchema }
            }
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (getByIdOperation.Security is null)
            {
                getByIdOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            getByIdOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return getByIdOperation;
    }

    private OpenApiOperation PostOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiSchema schema = GetOrCreateSchemaForIncomingResource(openApiDocument, resourceAttribute);

        OpenApiMediaType mediaType = new()
        {
            Schema = schema
        };

        OpenApiOperation postOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".Create",
            Summary = "Add a new resource to a collection.",
            Tags = GetTagsForCollection(openApiDocument, collectionName),
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypes.Json] = mediaType,
                    [MediaTypes.Ion] = mediaType
                }
            }
        };

        if (postOperation.Responses is null)
        {
            postOperation.Responses = new OpenApiResponses();
        }

        postOperation.Responses.Add("201", new OpenApiResponse
        {
            Description = "Successfully created"
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (postOperation.Security is null)
            {
                postOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            postOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return postOperation;
    }

    private OpenApiOperation PutOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiSchema schema = GetOrCreateSchemaForIncomingResource(openApiDocument, resourceAttribute);

        OpenApiMediaType mediaType = new()
        {
            Schema = schema
        };

        OpenApiOperation putOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".Update",
            Summary = "Updates an existing resource.",
            Tags = GetTagsForCollection(openApiDocument, collectionName),
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypes.Json] = mediaType,
                    [MediaTypes.Ion] = mediaType
                }
            },
            Parameters = new List<IOpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id",
                    In = ParameterLocation.Path,
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = JsonSchemaType.String,
                        Format = "uuid"
                    }
                }
            }
        };

        if (putOperation.Responses is null)
        {
            putOperation.Responses = new OpenApiResponses();
        }

        putOperation.Responses.Add("204", new OpenApiResponse
        {
            Description = "Successfully updated"
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            if (putOperation.Security is null)
            {
                putOperation.Security = new List<OpenApiSecurityRequirement>();
            }

            putOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("bearerAuth"), new List<string>()
                }
            });
        }

        return putOperation;
    }

    /// <summary>
    /// Converts collection urls like "users" or "employee-contracts" to Pascal cased spaced tag names like "Users" or "Employee Contracts".
    /// </summary>
    /// <param name="openApiDocument">OpenApi document</param>
    /// <param name="collectionName">Lower case hyphenated url</param>
    /// <returns></returns>
    private static ISet<OpenApiTagReference> GetTagsForCollection(OpenApiDocument openApiDocument, string collectionName)
    {
        string[] tokens = collectionName.Split('-', StringSplitOptions.RemoveEmptyEntries);

        StringBuilder stringBuilder = new();

        bool hasTokens = false;

        foreach (string token in tokens)
        {
            if (hasTokens)
            {
                stringBuilder.Append(' ');
            }
            stringBuilder.Append(token.ToPascalCase());
            hasTokens = true;
        }

        OpenApiTag tag = new()
        {
            Name = stringBuilder.ToString()
        };

        if (openApiDocument.Tags is null)
        {
            openApiDocument.Tags = new HashSet<OpenApiTag>();
        }

        openApiDocument.Tags.Add(tag);

        OpenApiTagReference tagReference = new("tag-" + collectionName, openApiDocument);

        return new HashSet<OpenApiTagReference>
        {
            tagReference
        };
    }

    /// <summary>
    /// Converts collection urls like "users" or "employee-contracts" to Pascal cased closed operation prefix names like "Users" or "EmployeeContracts".
    /// </summary>
    /// <param name="collectionName">Lower case hyphenated url</param>
    /// <returns></returns>
    private static string CollectionNameToOperationPrefix(string collectionName)
    {
        string[] tokens = collectionName.Split('-', StringSplitOptions.RemoveEmptyEntries);

        StringBuilder stringBuilder = new();

        foreach (string token in tokens)
        {
            stringBuilder.Append(token.ToPascalCase());
        }

        return stringBuilder.ToString();
    }

    private OpenApiSchema GetOrCreateSchemaForIncomingResource(OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute)
    {
        string componentSchemaName = resourceAttribute.ResourceType.Name + "Request";

        OpenApiSchema schemaWithReference = OpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute, componentSchemaName);

        return schemaWithReference;
    }

    private static OpenApiSchema GetOrCreateSchemaForOutgoingResource(OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute, Dictionary<string, IOpenApiSchema> documentPropertiesForMediaType, string mediaTypeSuffix, bool isCollection = false)
    {
        string componentSchemaName = resourceAttribute.ResourceType.Name + (isCollection ? "Collection" : string.Empty) + "Response_" + mediaTypeSuffix;

        OpenApiSchema schemaWithReference = openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () =>
            new OpenApiSchema
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = true,
                Properties = documentPropertiesForMediaType
            });

        return schemaWithReference;
    }

    private static OpenApiSchema GetOrCreateSchemaForForms(OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute, Dictionary<string, IOpenApiSchema> documentPropertiesForMediaType, string mediaTypeSuffix)
    {
        string componentSchemaName = resourceAttribute.ResourceType.Name + "Form_" + mediaTypeSuffix;

        OpenApiSchema schemaWithReference = openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () =>
            new OpenApiSchema()
            {
                Type = JsonSchemaType.Object,
                AdditionalPropertiesAllowed = true,
                Properties = documentPropertiesForMediaType
            });

        return schemaWithReference;
    }
}
