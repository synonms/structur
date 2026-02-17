using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Synonms.Structur.Api.Server.Tests.Unit.Shared;

internal static class TestHttpContextFactory
{
    public static HttpContext CreateWithHeader(string headerName, string value) =>
        new DefaultHttpContext()
        {
            Request =
            {
                Headers =
                {
                    [headerName] = value
                }
            }
        };

    public static HttpContext CreateWithItem(string itemName, object value) =>
        new DefaultHttpContext()
        {
            Items = 
            {
                [itemName] = value
            }
        };

    public static HttpContext CreateWithQueryString(string queryStringKey, string apiVersion) =>
        new DefaultHttpContext()
        {
            Request =
            {
                Query = new QueryCollection(
                    new Dictionary<string, StringValues> 
                    {
                        [queryStringKey] = apiVersion
                    })
            }
        };
}