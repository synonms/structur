using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Synonms.Structur.Api.Client.Http.Requests;
using Synonms.Structur.Api.Client.Http.Responses;
using Synonms.Structur.Api.Core.Faults;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Core.Schema.Client;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Client.Http;

public class StructurHttpClient<TResource> where TResource : Resource, new()
{
    private readonly HttpClient _httpClient;
    private readonly string _collectionPath;
    
    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { 
            new DateOnlyJsonConverter(),
            new OptionalDateOnlyJsonConverter(),
            new TimeOnlyJsonConverter(),
            new OptionalTimeOnlyJsonConverter(),
            new DefaultCustomJsonConverterFactory(),
            new DefaultLinkJsonConverter(),
            new DefaultFormDocumentJsonConverter(),
            new DefaultFormFieldJsonConverter(),
            new DefaultPaginationJsonConverter(),
            new DefaultErrorCollectionDocumentJsonConverter(),
            new DefaultErrorJsonConverter()
        }
    };
    
    public StructurHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _collectionPath = new TResource().GetCollectionPath();
    }
    
    public async Task<Result<ResourceCollectionDocument<TResource>>> GetAllAsync(GetAllRequest request, CancellationToken cancellationToken = default)
    {
        Dictionary<string, string> queryParameters = request.QueryParameters;
        if (request.Offset > 0) queryParameters.Add("offset", request.Offset.Value.ToString());
        if (request.Limit > 0) queryParameters.Add("limit", request.Limit.Value.ToString());

        string queryString = queryParameters.ToQueryString();

        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }
        
        using HttpResponseMessage response = await _httpClient.GetAsync(_collectionPath + queryString, cancellationToken);

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
        }

        ResourceCollectionResponse<TResource>? body = JsonSerializer.Deserialize<ResourceCollectionResponse<TResource>>(content, _jsonSerializerOptions);

        if (body is null)
        {
            return new ApiFault("Unable to deserialise response body.");
        }

        return body.Match(
            errorCollectionDocument => new ApiFault(errorCollectionDocument.Errors),
            Result<ResourceCollectionDocument<TResource>>.Success);
    }
    
    public async Task<Result<ResourceDocument<TResource>>> GetByIdAsync(GetByIdRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }
        
        using HttpResponseMessage response = await _httpClient.GetAsync($"{_collectionPath}/{request.Id}", cancellationToken);

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
        }

        ResourceResponse<TResource>? body = JsonSerializer.Deserialize<ResourceResponse<TResource>>(content, _jsonSerializerOptions);

        if (body is null)
        {
            return new ApiFault("Unable to deserialise response body.");
        }

        return body.Match(
            errorCollectionDocument => new ApiFault(errorCollectionDocument.Errors),
            Result<ResourceDocument<TResource>>.Success);
    }
    
    public async Task<Result<PostResponse>> PostAsync(PostRequest<TResource> request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }
        
        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(_collectionPath, request.Resource, _jsonSerializerOptions, cancellationToken);

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            Guid? id = response.ExtractResourceId();

            if (id is null)
            {
                return new ApiFault("Received status code '{StatusCode}' but unable to extract new Id from response. Content: \n{Content}.",  (int)response.StatusCode, content);
            }
            
            Guid? entityTag = response.ExtractEntityTag();

            if (entityTag is null)
            {
                return new ApiFault("Received status code '{StatusCode}' but unable to extract new EntityTag from response. Content: \n{Content}.",  (int)response.StatusCode, content);
            }

            return new PostResponse
            {
                Id = id.Value,
                EntityTag = entityTag.Value
            };
        }

        ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(content, _jsonSerializerOptions);

        if (errorCollectionDocument is not null)
        {
            return new ApiFault(errorCollectionDocument.Errors);
        }
        
        return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
    }
    
    public async Task<Result<PutResponse>> PutAsync(PutRequest<TResource> request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }
        
        using HttpResponseMessage response = await _httpClient.PutAsJsonAsync($"{_collectionPath}/{request.Id}", request.Resource, _jsonSerializerOptions, cancellationToken);

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            Guid? entityTag = response.ExtractEntityTag();

            if (entityTag is null)
            {
                return new ApiFault("Received status code '{StatusCode}' but unable to extract latest EntityTag from response. Content: \n{Content}.",  (int)response.StatusCode, content);
            }

            return new PutResponse
            {
                EntityTag = entityTag.Value
            };
        }

        ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(content, _jsonSerializerOptions);

        if (errorCollectionDocument is not null)
        {
            return new ApiFault(errorCollectionDocument.Errors);
        }
        
        return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
    }
    
    public async Task<Maybe<Fault>> DeleteAsync(DeleteRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }
        
        using HttpResponseMessage response = await _httpClient.DeleteAsync($"{_collectionPath}/{request.Id}", cancellationToken);

        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        
        if (response.IsSuccessStatusCode)
        {
            return Maybe<Fault>.None;
        }

        ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(content, _jsonSerializerOptions);

        if (errorCollectionDocument is not null)
        {
            return new ApiFault(errorCollectionDocument.Errors);
        }
        
        return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
    }}