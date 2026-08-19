using System.Net;
using Xunit;

namespace Synonms.Structur.Testing.Extensions;

public static class HttpResponseMessageExtensions
{
    public static HttpResponseMessage AssertFailure(this HttpResponseMessage httpResponseMessage, HttpStatusCode expectedStatusCode)
    {
        Assert.False(httpResponseMessage.IsSuccessStatusCode, $"Expected failed HTTP response but received {httpResponseMessage.StatusCode}");
        Assert.Equal(expectedStatusCode, httpResponseMessage.StatusCode);

        return httpResponseMessage;
    }
    
    public static HttpResponseMessage AssertSuccess(this HttpResponseMessage httpResponseMessage, HttpStatusCode expectedStatusCode)
    {
        Assert.True(httpResponseMessage.IsSuccessStatusCode, $"Expected successful HTTP response but received {httpResponseMessage.StatusCode}");
        Assert.Equal(expectedStatusCode, httpResponseMessage.StatusCode);

        return httpResponseMessage;
    }
}