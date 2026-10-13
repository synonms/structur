using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Server.Versioning;
using Synonms.Structur.Api.Server.Versioning.Context;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Versioning;

public class RegistrationVersionFilterTests
{
    [Fact]
    public async Task OnActionExecutionAsync_GivenUnsupportedExplicitVersion_ReturnsBadRequestWithoutExecutingAction()
    {
        ActionExecutingContext context = CreateContext(new Version(2, 0));
        RegistrationVersionFilter filter = new(typeof(TestResource), new HashSet<Version> { new(1, 0) });
        bool actionExecuted = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            actionExecuted = true;
            throw new InvalidOperationException("The action must not execute.");
        });

        Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.False(actionExecuted);
    }

    [Fact]
    public async Task OnActionExecutionAsync_GivenSupportedVersion_ContinuesToAction()
    {
        ActionExecutingContext context = CreateContext(new Version(1, 0));
        RegistrationVersionFilter filter = new(typeof(TestResource), new HashSet<Version> { new(1, 0) });
        bool actionExecuted = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            actionExecuted = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task OnActionExecutionAsync_GivenNoExplicitVersion_ContinuesToAction()
    {
        ActionExecutingContext context = CreateContext(null);
        RegistrationVersionFilter filter = new(typeof(TestResource), new HashSet<Version> { new(1, 0) });
        bool actionExecuted = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            actionExecuted = true;
            return Task.FromResult(new ActionExecutedContext(context, [], new object()));
        });

        Assert.True(actionExecuted);
    }

    [Fact]
    public async Task OnActionExecutionAsync_GivenUnsupportedVersionHeader_ReturnsBadRequest()
    {
        ActionExecutingContext context = CreateContext(null);
        context.HttpContext.Request.Headers[HttpHeaders.ApiVersion] = "2.0";
        RegistrationVersionFilter filter = new(typeof(TestResource), new HashSet<Version> { new(1, 0) });

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("The action must not execute."));

        Assert.IsType<BadRequestObjectResult>(context.Result);
    }

    private static ActionExecutingContext CreateContext(Version? version)
    {
        ServiceCollection services = new();
        services.AddSingleton<IVersionContext>(new VersionContext { Version = version });

        DefaultHttpContext httpContext = new()
        {
            RequestServices = services.BuildServiceProvider()
        };
        ActionContext actionContext = new(httpContext, new RouteData(), new ActionDescriptor());
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
    }

    private sealed class TestResource
    {
    }
}
