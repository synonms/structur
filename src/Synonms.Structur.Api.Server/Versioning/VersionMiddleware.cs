using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Server.Versioning.Resolution;

namespace Synonms.Structur.Api.Server.Versioning;

public class VersionMiddleware(ILogger<VersionMiddleware> logger, IVersionContext versionContext, IVersionResolver versionResolver) : IMiddleware
{
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        logger.LogTrace("{ClassName}.{FunctionName}", nameof(VersionMiddleware), nameof(InvokeAsync));
        
        if (versionContext.Version is not null)
        {
            logger.LogDebug("Version already present - Version middleware complete.");
            await next(httpContext);
            return;
        }

        (await versionResolver.ResolveAsync())
            .Match(
                version =>
                {
                    logger.LogInformation("Successfully determined Version {Version} from request.", version);
                    
                    versionContext.Version = version;
                },
                () => logger.LogInformation("Failed to determine Version from request."));

        await next(httpContext);
    }
}