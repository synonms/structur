using Microsoft.AspNetCore.Http;

namespace Synonms.Structur.WebApi.Http;

public class OptionsMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        if (httpContext.Request.Method == "OPTIONS")
        {
            httpContext.Response.Headers.AccessControlAllowOrigin = httpContext.Request.Headers.Origin;
            httpContext.Response.Headers.AccessControlAllowHeaders = new[] { "*" };
            httpContext.Response.Headers.AccessControlAllowMethods = new[] { "GET, POST, PUT, DELETE, OPTIONS" };
            httpContext.Response.Headers.AccessControlAllowCredentials = new[] { "true" };
            httpContext.Response.StatusCode = 200;
            await httpContext.Response.WriteAsync("OK");
            return;
        }

        await next(httpContext);
    }
}