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
    private readonly IProductContextAccessor<TProduct> _productContextAccessor;
    private readonly ITenantContextAccessor<TTenant> _tenantContextAccessor;

    public PermissionsMiddleware(
        ILogger<PermissionsMiddleware<TUser, TProduct, TTenant>> logger,
        IUserContextAccessor<TUser> userContextAccessor,
        IProductContextAccessor<TProduct> productContextAccessor, 
        ITenantContextAccessor<TTenant> tenantContextAccessor)
    {
        _logger = logger;
        _userContextAccessor = userContextAccessor;
        _productContextAccessor = productContextAccessor;
        _tenantContextAccessor = tenantContextAccessor;
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

        if (_productContextAccessor.ProductContext?.SelectedProduct is null)
        {
            _logger.LogDebug("No Product selected - Permissions middleware complete.");
            await next(httpContext);
            return;
        }

        if (_userContextAccessor.UserContext.AuthenticatedUser.PermissionsPerProductId.TryGetValue(_productContextAccessor.ProductContext.SelectedProduct.Id, out IEnumerable<StructurUser.UserPermission>? permissions) is false)
        {
            _logger.LogDebug("User Id '{userId}' has no Permissions available for Product Id '{productId}'.", _userContextAccessor.UserContext.AuthenticatedUser.Id, _productContextAccessor.ProductContext.SelectedProduct.Id);
            await next(httpContext);
            return;
        }

        Guid? tenantId = _tenantContextAccessor.TenantContext?.SelectedTenant?.Id;
        
        List<Claim> claims = permissions
            .Where(permission => permission.TenantId is null || (tenantId is not null && permission.TenantId == tenantId))
            .Select(permission => new Claim(Permissions.ClaimType, permission.Value)).ToList();
        ClaimsIdentity claimsIdentity = new(claims);

        httpContext.User.AddIdentity(claimsIdentity);
        
        _logger.LogDebug("Permissions middleware complete.");
        await next(httpContext);
    }
}