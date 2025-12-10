using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Synonms.Structur.Application.Iana;
using Synonms.Structur.Application.Routing;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System;
using Synonms.Structur.WebApi.Content;
using Synonms.Structur.WebApi.OpenApi.Default;
using Synonms.Structur.WebApi.OpenApi.Ion;
using Synonms.Structur.WebApi.Serialisation.Default;
using Synonms.Structur.WebApi.Serialisation.Ion;

namespace Synonms.Structur.WebApi.OpenApi;

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
        IReadOnlyDictionary<string, IResourceDirectory.AggregateRootLayout> aggregateRootLayouts = _resourceDirectory.GetAllRoots();

        foreach ((string collectionName, IResourceDirectory.AggregateRootLayout aggregateRootLayout) in aggregateRootLayouts)
        {
            StructurResourceAttribute? resourceAttribute = aggregateRootLayout.AggregateRootType.GetCustomAttribute<StructurResourceAttribute>();

            if (resourceAttribute is null)
            {
                continue;
            }
            
            OpenApiOperation getAllOperation = GetAllOperation(openApiDocument, collectionName, resourceAttribute);

            OpenApiPathItem resourceCollectionPathItem = new() 
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>()
                {
                    [OperationType.Get] = getAllOperation
                }
            };
            
            if (resourceAttribute.IsCreateDisabled is false)
            {
                OpenApiOperation postOperation = PostOperation(openApiDocument, collectionName, resourceAttribute);

                resourceCollectionPathItem.Operations.Add(OperationType.Post, postOperation);
                
                OpenApiOperation createFormOperation = CreateFormOperation(openApiDocument, collectionName, resourceAttribute);
                
                OpenApiPathItem createFormPathItem = new() 
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>()
                    {
                        [OperationType.Get] = createFormOperation
                    }
                };
                
                openApiDocument.Paths.Add("/" + collectionName + "/create-form", createFormPathItem);
            }

            openApiDocument.Paths.Add("/" + collectionName, resourceCollectionPathItem);
            
            OpenApiOperation getByIdOperation = GetByIdOperation(openApiDocument, collectionName, resourceAttribute);
            
            OpenApiPathItem resourcePathItem = new() 
            {
                Operations = new Dictionary<OperationType, OpenApiOperation>()
                {
                    [OperationType.Get] = getByIdOperation
                }
            };

            if (resourceAttribute.IsUpdateDisabled is false)
            {
                OpenApiOperation putOperation = PutOperation(openApiDocument, collectionName, resourceAttribute);

                resourcePathItem.Operations.Add(OperationType.Put, putOperation);
                
                OpenApiOperation editFormOperation = EditFormOperation(openApiDocument, collectionName, resourceAttribute);
                
                OpenApiPathItem editFormPathItem = new() 
                {
                    Operations = new Dictionary<OperationType, OpenApiOperation>()
                    {
                        [OperationType.Get] = editFormOperation
                    }
                };
                
                openApiDocument.Paths.Add("/" + collectionName + "/{id}/edit-form", editFormPathItem);
            }

            if (resourceAttribute.IsDeleteDisabled is false)
            {
                OpenApiOperation deleteOperation = DeleteOperation(collectionName, resourceAttribute);

                resourcePathItem.Operations.Add(OperationType.Delete, deleteOperation);
            }

            openApiDocument.Paths.Add("/" + collectionName + "/{id}", resourcePathItem);
        }
        
        return Task.CompletedTask;
    }

    private OpenApiOperation CreateFormOperation(OpenApiDocument openApiDocument, string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation createFormOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".CreateForm",
            Summary = "Get a form describing how to add a new resource to a collection.",
            Tags = GetTagsForCollection(collectionName)
        };

        Dictionary<string, OpenApiSchema> defaultCreateFormProperties = new()
        {
            {
                DefaultPropertyNames.Value, new OpenApiSchema
                {
                    Type = "array", 
                    Items = DefaultOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, defaultCreateFormProperties, "Default");

        Dictionary<string, OpenApiSchema> ionCreateFormProperties = new()
        {
            { IonPropertyNames.Links.Uri, new OpenApiSchema() { Type = "string", Format = "uri" } },
            { IonPropertyNames.Links.Relation, new OpenApiSchema() { Type = "string" } },
            { IonPropertyNames.Links.Method, new OpenApiSchema() { Type = "string" } },
            {
                IonPropertyNames.Value, new OpenApiSchema()
                {
                    Type = "array", 
                    Items = IonOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, ionCreateFormProperties, "Ion");

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
            createFormOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
                }
            });
        }

        return createFormOperation;
    }

    private OpenApiOperation DeleteOperation(string collectionName, StructurResourceAttribute resourceAttribute)
    {
        OpenApiOperation deleteOperation = new()
        {
            OperationId = CollectionNameToOperationPrefix(collectionName) + ".Delete",
            Summary = "Deletes an existing resource.",
            Tags = GetTagsForCollection(collectionName),
            Parameters = new List<OpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id", 
                    In = ParameterLocation.Path, 
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                } 
            }
        };

        deleteOperation.Responses.Add("204", new OpenApiResponse
        {
            Description = "Successfully deleted"
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            deleteOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
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
            Tags = GetTagsForCollection(collectionName),
            Parameters = new List<OpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id", 
                    In = ParameterLocation.Path, 
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                } 
            }
        };

        Dictionary<string, OpenApiSchema> defaultEditFormProperties = new()
        {
            {
                DefaultPropertyNames.Value, new OpenApiSchema
                {
                    Type = "array", 
                    Items = DefaultOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, defaultEditFormProperties, "Default");

        Dictionary<string, OpenApiSchema> ionCreateFormProperties = new()
        {
            { IonPropertyNames.Links.Uri, new OpenApiSchema() { Type = "string", Format = "uri" } },
            { IonPropertyNames.Links.Relation, new OpenApiSchema() { Type = "string" } },
            { IonPropertyNames.Links.Method, new OpenApiSchema() { Type = "string" } },
            {
                IonPropertyNames.Value, new OpenApiSchema()
                {
                    Type = "array", 
                    Items = IonOpenApiSchemaFactory.CreateForFormField()
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForForms(openApiDocument, resourceAttribute, ionCreateFormProperties, "Ion");

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
            editFormOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
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
            Tags = GetTagsForCollection(collectionName)
        };

        Dictionary<string, OpenApiSchema> defaultResourceCollectionDocumentProperties = new()
        {
            {
                DefaultPropertyNames.Value, new OpenApiSchema
                {
                    Type = "array", 
                    Items = DefaultOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute)
                }
            },
            { DefaultPropertyNames.Pagination.Offset, new OpenApiSchema() { Type = "integer" } },
            { DefaultPropertyNames.Pagination.Limit, new OpenApiSchema() { Type = "integer" } },
            { DefaultPropertyNames.Pagination.Size, new OpenApiSchema() { Type = "integer" } }
        };
        
        OpenApiSchema defaultSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, defaultResourceCollectionDocumentProperties, "Default", true);
        
        Dictionary<string, OpenApiSchema> ionResourceCollectionDocumentProperties = new()
        {
            {
                IonPropertyNames.Value, new OpenApiSchema()
                {
                    Type = "array", 
                    Items = IonOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute)
                }
            },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() },
            { "first", IonOpenApiSchemaFactory.CreateForLink() },
            { "previous", IonOpenApiSchemaFactory.CreateForLink() },
            { "next", IonOpenApiSchemaFactory.CreateForLink() },
            { "last", IonOpenApiSchemaFactory.CreateForLink() },
            { "offset", new OpenApiSchema() { Type = "integer" } },
            { "limit", new OpenApiSchema() { Type = "integer" } },
            { "size", new OpenApiSchema() { Type = "integer" } }
        };
        
        OpenApiSchema ionSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, ionResourceCollectionDocumentProperties, "Ion", true);

        if (resourceAttribute.IsCreateDisabled is false)
        {
            ionResourceCollectionDocumentProperties.Add("create-form", IonOpenApiSchemaFactory.CreateForLink());
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
            getAllOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
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
            Tags = GetTagsForCollection(collectionName),
            Parameters = new List<OpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id", 
                    In = ParameterLocation.Path, 
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                } 
            }
        };

        Dictionary<string, OpenApiSchema> defaultResourceDocumentProperties = new()
        {
            { DefaultPropertyNames.Value, DefaultOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute) },
            { IanaLinkRelationConstants.Self, DefaultOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema defaultSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, defaultResourceDocumentProperties, "Default");

        Dictionary<string, OpenApiSchema> ionResourceDocumentProperties = new()
        {
            { IonPropertyNames.Value, IonOpenApiSchemaFactory.GetOrCreateSchemaReferenceForResource(_logger, openApiDocument, resourceAttribute) },
            { IanaLinkRelationConstants.Self, IonOpenApiSchemaFactory.CreateForLink() }
        };

        OpenApiSchema ionSchema = GetOrCreateSchemaForOutgoingResource(openApiDocument, resourceAttribute, ionResourceDocumentProperties, "Ion");

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
            getByIdOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
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
            Tags = GetTagsForCollection(collectionName),
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypes.Json] = mediaType,
                    [MediaTypes.Ion] = mediaType
                }
            }
        };

        postOperation.Responses.Add("201", new OpenApiResponse
        {
            Description = "Successfully created"
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            postOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
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
            Tags = GetTagsForCollection(collectionName),
            RequestBody = new OpenApiRequestBody
            {
                Content = new Dictionary<string, OpenApiMediaType>
                {
                    [MediaTypes.Json] = mediaType,
                    [MediaTypes.Ion] = mediaType
                }
            },
            Parameters = new List<OpenApiParameter>()
            {
                new OpenApiParameter()
                {
                    Name = "id", 
                    In = ParameterLocation.Path, 
                    Required = true,
                    Description = "Unique identifier of the resource.",
                    Schema = new OpenApiSchema
                    {
                        Type = "string",
                        Format = "uuid"
                    }
                } 
            }
        };

        putOperation.Responses.Add("204", new OpenApiResponse
        {
            Description = "Successfully updated"
        });

        if (resourceAttribute.AllowAnonymous is false)
        {
            putOperation.Security.Add(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }, new List<string>()
                }
            });
        }

        return putOperation;
    }
    
    /// <summary>
    /// Converts collection urls like "users" or "employee-contracts" to Pascal cased spaced tag names like "Users" or "Employee Contracts".
    /// </summary>
    /// <param name="collectionName">Lower case hyphenated url</param>
    /// <returns></returns>
    private static List<OpenApiTag> GetTagsForCollection(string collectionName)
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

        return [tag];
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
    
    private static OpenApiSchema GetOrCreateSchemaForOutgoingResource(OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute, Dictionary<string, OpenApiSchema> documentPropertiesForMediaType, string mediaTypeSuffix, bool isCollection = false)
    {
        string componentSchemaName = resourceAttribute.ResourceType.Name + (isCollection ? "Collection" : string.Empty) + "Response_" + mediaTypeSuffix;

        OpenApiSchema schemaWithReference = openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () => 
            new OpenApiSchema
            {
                Type = "object",
                AdditionalPropertiesAllowed = true,
                Properties = documentPropertiesForMediaType
            });
        
        return schemaWithReference;
    }
    
    private static OpenApiSchema GetOrCreateSchemaForForms(OpenApiDocument openApiDocument, StructurResourceAttribute resourceAttribute, Dictionary<string, OpenApiSchema> documentPropertiesForMediaType, string mediaTypeSuffix)
    {
        string componentSchemaName = resourceAttribute.ResourceType.Name + "Form_" + mediaTypeSuffix;

        OpenApiSchema schemaWithReference = openApiDocument.GetOrCreateSchemaReference(componentSchemaName, () => 
            new OpenApiSchema()
            {
                Type = "object",
                AdditionalPropertiesAllowed = true,
                Properties = documentPropertiesForMediaType
            });
        
        return schemaWithReference;
    }
}