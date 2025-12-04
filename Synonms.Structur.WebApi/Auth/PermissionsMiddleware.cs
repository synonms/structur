using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Products;
using Synonms.Structur.Application.Products.Context;
using Synonms.Structur.Application.Tenants;
using Synonms.Structur.Application.Tenants.Context;
using Synonms.Structur.Application.Users;
using Synonms.Structur.Application.Users.Context;

namespace Synonms.Structur.WebApi.Auth;

public class PermissionsMiddleware<TUser, TProduct, TTenant> : IMiddleware
    where TUser : StructurUser
    where TProduct : StructurProduct
    where TTenant : StructurTenant
{
    private readonly ILogger<PermissionsMiddleware<TUser, TProduct, TTenant>> _logger;
    private readonly IUserContextAccessor<TUser> _userContextAccessor;
    private readonly IProductContext<TProduct> _productContext;
    private readonly ITenantContext<TTenant> _tenantContext;

    public PermissionsMiddleware(
        ILogger<PermissionsMiddleware<TUser, TProduct, TTenant>> logger,
        IUserContextAccessor<TUser> userContextAccessor,
        IProductContext<TProduct> productContext, 
        ITenantContext<TTenant> tenantContext)
    {
        _logger = logger;
        _userContextAccessor = userContextAccessor;
        _productContext = productContext;
        _tenantContext = tenantContext;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogDebug("Executing Permissions Middleware...");
        
        if (_userContextAccessor.UserContext?.AuthenticatedUser is null)
        {
            _logger.LogDebug("No Authenticated User present - Permissions middleware complete.");
            await next(httpContext);
            return;
        }

        _productContext.GetProduct()
            .Match(
                product =>
                {
                    if (_userContextAccessor.UserContext.AuthenticatedUser.PermissionsPerProductId.TryGetValue(product.Id, out IEnumerable<StructurUser.UserPermission>? permissions) is false)
                    {
                        _logger.LogDebug("User Id '{userId}' has no Permissions available for Product Id '{productId}' - Permissions middleware complete.", _userContextAccessor.UserContext.AuthenticatedUser.Id, product.Id);
                    }
                    else
                    {
                        Guid? tenantId = _tenantContext.GetTenantId();
                        
                        List<Claim> claims = permissions
                            .Where(permission => permission.TenantId is null || (tenantId is not null && permission.TenantId == tenantId))
                            .Select(permission => new Claim(Permissions.ClaimType, permission.Value)).ToList();
                        ClaimsIdentity claimsIdentity = new(claims);

                        httpContext.User.AddIdentity(claimsIdentity);
                        
                        _logger.LogDebug("{ClaimsCount} permission claims added - Permissions middleware complete.", claims.Count);
                    }
                },
                fault => _logger.LogDebug("No Product selected - Permissions middleware complete."));

        await next(httpContext);
    }
}