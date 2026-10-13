using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Reflection;
using Synonms.Structur.Api.Client.Http.Requests;
using Synonms.Structur.Api.Client.Http.Responses;
using Synonms.Structur.Api.Core.Faults;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Client;
using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Client.Http;

public class StructurHttpClient<TResource>
{
    private readonly HttpClient _httpClient;
    private readonly string _collectionPath;

    public StructurHttpClient(HttpClient httpClient, string? collectionPath = null)
    {
        _httpClient = httpClient;
        _collectionPath = collectionPath ?? GetCollectionPath();
    }

    private static string GetCollectionPath()
    {
        FieldInfo? pathField = typeof(TResource).GetField("CollectionPath", BindingFlags.Public | BindingFlags.Static);
        if (pathField?.GetRawConstantValue() is string path && !string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        throw new InvalidOperationException(
            $"A collection path must be provided for plain resource contract {typeof(TResource).FullName}, or exposed as a public const string CollectionPath.");
    }

    public async Task<Result<ResourceCollectionDocument<TResource>>> GetAllAsync(GetAllRequest request, Version? version = null, CancellationToken cancellationToken = default)
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

        JsonSerializerOptions jsonSerialiserOptions = BuildJsonSerializerOptions(version);

        ResourceCollectionResponse<TResource>? body = JsonSerializer.Deserialize<ResourceCollectionResponse<TResource>>(content, jsonSerialiserOptions);

        if (body is null)
        {
            return new ApiFault("Unable to deserialise response body.");
        }

        return body.Match(
            errorCollectionDocument => new ApiFault(errorCollectionDocument.Errors),
            Result<ResourceCollectionDocument<TResource>>.Success);
    }

    public async Task<Result<ResourceDocument<TResource>>> GetByIdAsync(GetByIdRequest request, Version? version = null, CancellationToken cancellationToken = default)
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

        JsonSerializerOptions jsonSerialiserOptions = BuildJsonSerializerOptions(version);

        ResourceResponse<TResource>? body = JsonSerializer.Deserialize<ResourceResponse<TResource>>(content, jsonSerialiserOptions);

        if (body is null)
        {
            return new ApiFault("Unable to deserialise response body.");
        }

        return body.Match(
            errorCollectionDocument => new ApiFault(errorCollectionDocument.Errors),
            Result<ResourceDocument<TResource>>.Success);
    }

    public async Task<Result<PostResponse>> PostAsync(PostRequest<TResource> request, Version? version = null, CancellationToken cancellationToken = default)
    {
        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }

        JsonSerializerOptions jsonSerialiserOptions = BuildJsonSerializerOptions(version);

        ResourceMetadata metadata = request.Metadata ?? new ResourceMetadata(Guid.NewGuid(), Link.EmptyLink());
        using HttpContent requestContent = CreateResourceContent(new ResourceEnvelope<TResource>(request.Resource, metadata), jsonSerialiserOptions, version);
        using HttpResponseMessage response = await _httpClient.PostAsync(_collectionPath, requestContent, cancellationToken);

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

        ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(content, jsonSerialiserOptions);

        if (errorCollectionDocument is not null)
        {
            return new ApiFault(errorCollectionDocument.Errors);
        }

        return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
    }

    public async Task<Result<PutResponse>> PutAsync(PutRequest<TResource> request, Version? version = null, CancellationToken cancellationToken = default)
    {
        if (request.TenantId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, request.TenantId.Value.ToString());
        }
        if (request.ProductId.HasValue)
        {
            _httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, request.ProductId.Value.ToString());
        }

        JsonSerializerOptions jsonSerialiserOptions = BuildJsonSerializerOptions(version);

        ResourceMetadata metadata = request.Metadata ?? new ResourceMetadata(request.Id, Link.EmptyLink());
        using HttpContent requestContent = CreateResourceContent(new ResourceEnvelope<TResource>(request.Resource, metadata), jsonSerialiserOptions, version);
        using HttpResponseMessage response = await _httpClient.PutAsync($"{_collectionPath}/{request.Id}", requestContent, cancellationToken);

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

        ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(content, jsonSerialiserOptions);

        if (errorCollectionDocument is not null)
        {
            return new ApiFault(errorCollectionDocument.Errors);
        }

        return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
    }

    public async Task<Maybe<Fault>> DeleteAsync(DeleteRequest request, Version? version = null, CancellationToken cancellationToken = default)
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

        ErrorCollectionDocument? errorCollectionDocument = JsonSerializer.Deserialize<ErrorCollectionDocument>(content, BuildJsonSerializerOptions(version));

        if (errorCollectionDocument is not null)
        {
            return new ApiFault(errorCollectionDocument.Errors);
        }

        return new ApiFault("Received status code '{StatusCode}' with content: \n{Content}.",  (int)response.StatusCode, content);
    }

    private static JsonSerializerOptions BuildJsonSerializerOptions(Version? version) => new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {
            new DateOnlyJsonConverter(),
            new OptionalDateOnlyJsonConverter(),
            new TimeOnlyJsonConverter(),
            new OptionalTimeOnlyJsonConverter(),
            new DefaultCustomJsonConverterFactory(version ?? new Version()),
            new DefaultLinkJsonConverter(),
            new DefaultFormDocumentJsonConverter(),
            new DefaultFormFieldJsonConverter(),
            new DefaultPaginationJsonConverter(),
            new DefaultErrorCollectionDocumentJsonConverter(),
            new DefaultErrorJsonConverter()
        }
    };

    private static HttpContent CreateResourceContent(
        ResourceEnvelope<TResource> envelope,
        JsonSerializerOptions options,
        Version? version)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            ResourceJsonWriter.Write(writer, envelope, options, version ?? new Version(), false);
        }

        return new StringContent(Encoding.UTF8.GetString(stream.ToArray()), Encoding.UTF8, "application/json");
    }
}
