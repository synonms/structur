using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Synonms.Structur.WebApi.Correlation;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Tests.Unit.Shared;
using Xunit;

namespace Synonms.Structur.WebApi.Tests.Unit.Correlation;

public class CorrelationMiddlewareTests
{
    private readonly ILogger<CorrelationMiddleware> _mockLogger = Substitute.For<ILogger<CorrelationMiddleware>>();
    
    [Fact]
    public async Task InvokeAsync_CallsNextDelegate()
    {
        DefaultHttpContext httpContext = new ();
        
        bool isDelegateCalled = false;

        CorrelationMiddleware middleware = new(_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(isDelegateCalled);
        return;

        Task Next(HttpContext _)
        {
            isDelegateCalled = true;
            return Task.CompletedTask;
        }
    }
    
    [Fact]
    public async Task InvokeAsync_IncomingCorrelationIdHeader_SetsExistingCorrelationIdInHttpContextItem()
    {
        Guid expectedCorrelationId = Guid.NewGuid();
        HttpContext httpContext = TestHttpContextFactory.CreateWithHeader(HttpHeaders.CorrelationId, expectedCorrelationId.ToString());
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new(_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Items.ContainsKey(HttpContextItemKeys.CorrelationId));
        Assert.True(Guid.TryParse(httpContext.Items[HttpContextItemKeys.CorrelationId]!.ToString(), out Guid actualCorrelationId));
        Assert.Equal(expectedCorrelationId, actualCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_IncomingRequestIdHeader_SetsNewRequestIdInHttpContextItem()
    {
        Guid incomingRequestId = Guid.NewGuid();
        HttpContext httpContext = TestHttpContextFactory.CreateWithHeader(HttpHeaders.RequestId, incomingRequestId.ToString());
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new(_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Items.ContainsKey(HttpContextItemKeys.RequestId));
        Assert.True(Guid.TryParse(httpContext.Items[HttpContextItemKeys.RequestId]!.ToString(), out Guid actualRequestId));
        Assert.NotEqual(Guid.Empty, actualRequestId);
        Assert.NotEqual(incomingRequestId, actualRequestId);
    }
    
    [Fact]
    public async Task InvokeAsync_NoIncomingCorrelationIdHeader_SetsNewCorrelationIdInHttpContextItem()
    {
        DefaultHttpContext httpContext = new ();
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new(_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Items.ContainsKey(HttpContextItemKeys.CorrelationId));
        Assert.True(Guid.TryParse(httpContext.Items[HttpContextItemKeys.CorrelationId]!.ToString(), out Guid actualCorrelationId));
        Assert.NotEqual(Guid.Empty, actualCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_NoIncomingRequestIdHeader_SetsNewRequestIdInHttpContextItem()
    {
        DefaultHttpContext httpContext = new ();
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new (_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Items.ContainsKey(HttpContextItemKeys.RequestId));
        Assert.True(Guid.TryParse(httpContext.Items[HttpContextItemKeys.RequestId]!.ToString(), out Guid actualRequestId));
        Assert.NotEqual(Guid.Empty, actualRequestId);
    }

    [Fact]
    public async Task InvokeAsync_IncomingCorrelationIdHeader_SetsExistingCorrelationIdInResponseHeader()
    {
        Guid expectedCorrelationId = Guid.NewGuid();
        HttpContext httpContext = TestHttpContextFactory.CreateWithHeader(HttpHeaders.CorrelationId, expectedCorrelationId.ToString());
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new (_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Response.Headers.ContainsKey(HttpHeaders.CorrelationId));
        Assert.True(Guid.TryParse(httpContext.Response.Headers[HttpHeaders.CorrelationId].ToString(), out Guid actualCorrelationId));
        Assert.Equal(expectedCorrelationId, actualCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_IncomingRequestIdHeader_SetsNewRequestIdInResponseHeader()
    {
        Guid incomingRequestId = Guid.NewGuid();
        HttpContext httpContext = TestHttpContextFactory.CreateWithHeader(HttpHeaders.RequestId, incomingRequestId.ToString());
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new (_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Response.Headers.ContainsKey(HttpHeaders.RequestId));
        Assert.True(Guid.TryParse(httpContext.Response.Headers[HttpHeaders.RequestId].ToString(), out Guid actualRequestId));
        Assert.NotEqual(Guid.Empty, actualRequestId);
        Assert.NotEqual(incomingRequestId, actualRequestId);
    }
    
    [Fact]
    public async Task InvokeAsync_NoIncomingCorrelationIdHeader_SetsNewCorrelationIdInResponseHeader()
    {
        DefaultHttpContext httpContext = new ();
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new (_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Response.Headers.ContainsKey(HttpHeaders.CorrelationId));
        Assert.True(Guid.TryParse(httpContext.Response.Headers[HttpHeaders.CorrelationId].ToString(), out Guid actualCorrelationId));
        Assert.NotEqual(Guid.Empty, actualCorrelationId);
    }

    [Fact]
    public async Task InvokeAsync_NoIncomingRequestIdHeader_SetsNewRequestIdInResponseHeader()
    {
        DefaultHttpContext httpContext = new ();
        Task Next(HttpContext _) => Task.CompletedTask;

        CorrelationMiddleware middleware = new (_mockLogger);

        await middleware.InvokeAsync(httpContext, Next);

        Assert.True(httpContext.Response.Headers.ContainsKey(HttpHeaders.RequestId));
        Assert.True(Guid.TryParse(httpContext.Response.Headers[HttpHeaders.RequestId].ToString(), out Guid actualRequestId));
        Assert.NotEqual(Guid.Empty, actualRequestId);
    }
}