using System.Net.Http.Headers;
using Synonms.Structur.Domain.Entities;
using HttpHeaders = Synonms.Structur.WebApi.Http.HttpHeaders;

namespace Synonms.Structur.Testing.Extensions;

public static class HttpClientExtensions
{
    public static HttpClient WithApiVersion(this HttpClient httpClient, int apiVersion)
    {
        httpClient.DefaultRequestHeaders.Add(HttpHeaders.ApiVersion, apiVersion.ToString());
        
        return httpClient;
    }

    public static HttpClient WithAuthenticatedUser(this HttpClient httpClient, string userId, params string[] permissions)
    {
        httpClient.DefaultRequestHeaders.Add(TestHttpHeaders.UserId, userId);

        foreach (string permission in permissions)
        {
            httpClient.DefaultRequestHeaders.Add(TestHttpHeaders.Permissions, permission);
        }
        
        return httpClient;
    }
    
    public static HttpClient WithBearerToken(this HttpClient httpClient, string bearerToken)
    {
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            
        return httpClient;
    }

    public static HttpClient WithEntityTag(this HttpClient httpClient, EntityTag entityTag)
    {
        httpClient.DefaultRequestHeaders.IfMatch.Clear();
        httpClient.DefaultRequestHeaders.IfMatch.Add(new EntityTagHeaderValue(entityTag.ToString()));

        return httpClient;
    }

    public static HttpClient WithCorrelationId(this HttpClient httpClient, Guid correlationId)
    {
        httpClient.DefaultRequestHeaders.Add(HttpHeaders.CorrelationId, correlationId.ToString());
        
        return httpClient;
    }
}