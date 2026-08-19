using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Api.Server.Tenants.Resolution;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Tenants;

public class TenantMiddleware<TUser, TTenant> : IMiddleware
    where TUser : StructurUser
    where TTenant : StructurTenant
{
    private readonly ILogger<TenantMiddleware<TUser, TTenant>> _logger;
    private readonly ITenantContext<TTenant> _tenantContext;
    private readonly ITenantIdResolver _tenantIdResolver;

    public TenantMiddleware(
        ILogger<TenantMiddleware<TUser, TTenant>> logger,
        ITenantContext<TTenant> tenantContext, 
        ITenantIdResolver tenantIdResolver)
    {
        _logger = logger;
        _tenantContext = tenantContext;
        _tenantIdResolver = tenantIdResolver;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogTrace("{ClassName}.{FunctionName}", nameof(TenantMiddleware<TUser, TTenant>), nameof(InvokeAsync));
        
        if (typeof(TTenant) == typeof(NoStructurTenant))
        {
            _logger.LogDebug("No Tenant required - Tenant middleware complete.");
            await next(httpContext);
            return;
        }
        
        if (_tenantContext.HasTenant())
        {
            _logger.LogDebug("Tenant already present - Tenant middleware complete.");
            await next(httpContext);
            return;
        }

        await _tenantIdResolver.ResolveAsync()
            .MatchAsync(
                async tenantId =>
                {
                    _logger.LogInformation("Successfully determined Tenant Id {TenantId} from request.", tenantId);
                    
                    await _tenantContext.SelectTenantAsync(tenantId, CancellationToken.None);

                    if (_tenantContext.HasTenant())
                    {
                        _logger.LogInformation("Successfully resolved Tenant Id {TenantId}.", tenantId);
                    }
                    else
                    {
                        _logger.LogWarning("Unable to resolve Tenant Id {TenantId}.", tenantId);
                    }
                },
                () => _logger.LogWarning("Failed to determine Tenant Id from request."));

        await next(httpContext);
    }
}