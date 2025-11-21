using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Tenants;
using Synonms.Structur.Application.Tenants.Context;
using Synonms.Structur.Application.Tenants.Resolution;
using Synonms.Structur.Application.Users;

namespace Synonms.Structur.WebApi.Tenants;

public class TenantMiddleware<TUser, TTenant> : IMiddleware
    where TUser : StructurUser
    where TTenant : StructurTenant
{
    private readonly ILogger<TenantMiddleware<TUser, TTenant>> _logger;
    private readonly ITenantContextFactory<TTenant> _tenantContextFactory;
    private readonly ITenantContextAccessor<TTenant> _tenantContextAccessor;
    private readonly ITenantIdResolver _tenantIdResolver;

    public TenantMiddleware(
        ILogger<TenantMiddleware<TUser, TTenant>> logger,
        ITenantContextFactory<TTenant> tenantContextFactory, 
        ITenantContextAccessor<TTenant> tenantContextAccessor, 
        ITenantIdResolver tenantIdResolver)
    {
        _logger = logger;
        _tenantContextFactory = tenantContextFactory;
        _tenantContextAccessor = tenantContextAccessor;
        _tenantIdResolver = tenantIdResolver;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogDebug("Executing Tenant Middleware...");
        
        if (_tenantContextAccessor.TenantContext is not null)
        {
            _logger.LogDebug("Tenant Context already present - Tenant middleware complete.");
            await next(httpContext);
            return;
        }

        Guid? selectedTenantId = null;
        
        (await _tenantIdResolver.ResolveAsync())
            .Match(
                tenantId =>
                {
                    _logger.LogDebug("Successfully resolved Tenant Id {tenantId}.", tenantId);
                    selectedTenantId = tenantId;
                },
                () => 
                {
                    _logger.LogDebug("Failed to resolve Tenant Id.");
                });

        _tenantContextAccessor.TenantContext = await _tenantContextFactory.CreateAsync(selectedTenantId, CancellationToken.None);
        
        _logger.LogDebug("Tenant middleware complete.");
        await next(httpContext);
    }
}