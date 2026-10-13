using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Server.Http;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Server.Versioning.Faults;

namespace Synonms.Structur.Api.Server.Versioning;

public sealed class RegistrationVersionFilter : IAsyncActionFilter
{
    private readonly Type _resourceType;
    private readonly IReadOnlySet<Version> _supportedVersions;

    public RegistrationVersionFilter(Type resourceType, IReadOnlySet<Version> supportedVersions)
    {
        _resourceType = resourceType;
        _supportedVersions = supportedVersions;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        Version? contextVersion = context.HttpContext.RequestServices
            .GetRequiredService<IVersionContext>()
            .Version;

        bool hasInvalidVersion;
        Version? requestedVersion = ResolveRequestedVersion(context.HttpContext.Request, contextVersion, out hasInvalidVersion);
        if (hasInvalidVersion || requestedVersion is not null && !_supportedVersions.Contains(requestedVersion))
        {
            IActionResult faultResult = HttpResponseMapper.MapFault(new ApplicableVersionFault(_resourceType));
            context.Result = faultResult;
            return;
        }

        await next();
    }

    private static Version? ResolveRequestedVersion(HttpRequest request, Version? contextVersion, out bool hasInvalidVersion)
    {
        List<string> explicitValues = [];
        hasInvalidVersion = false;

        AddHeaderVersion(request, HttpHeaders.ApiVersion, explicitValues, ref hasInvalidVersion);
        AddHeaderVersion(request, HttpHeaders.Version, explicitValues, ref hasInvalidVersion);

        if (request.Query.TryGetValue(HttpQueryStringKeys.ApiVersion, out StringValues queryVersions))
        {
            if (queryVersions.Count != 1)
            {
                hasInvalidVersion = true;
            }
            else
            {
                explicitValues.Add(queryVersions[0] ?? string.Empty);
            }
        }

        List<Version> parsedVersions = [];
        foreach (string explicitValue in explicitValues)
        {
            if (!Version.TryParse(explicitValue, out Version? parsedVersion))
            {
                hasInvalidVersion = true;
                continue;
            }

            parsedVersions.Add(parsedVersion);
        }

        if (parsedVersions.Distinct().Count() > 1)
        {
            hasInvalidVersion = true;
        }

        return parsedVersions.FirstOrDefault() ?? contextVersion;
    }

    private static void AddHeaderVersion(HttpRequest request, string headerName, ICollection<string> explicitValues, ref bool hasInvalidVersion)
    {
        if (!request.Headers.TryGetValue(headerName, out StringValues headerVersions))
        {
            return;
        }

        if (headerVersions.Count != 1)
        {
            hasInvalidVersion = true;
            return;
        }

        explicitValues.Add(headerVersions[0] ?? string.Empty);
    }
}
