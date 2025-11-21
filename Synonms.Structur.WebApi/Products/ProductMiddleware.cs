using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Products;
using Synonms.Structur.Application.Products.Context;
using Synonms.Structur.Application.Products.Resolution;
using Synonms.Structur.Application.Users;

namespace Synonms.Structur.WebApi.Products;

public class ProductMiddleware<TUser, TProduct> : IMiddleware
    where TUser : StructurUser
    where TProduct : StructurProduct
{
    private readonly ILogger<ProductMiddleware<TUser, TProduct>> _logger;
    private readonly IProductContextFactory<TProduct> _productContextFactory;
    private readonly IProductContextAccessor<TProduct> _productContextAccessor;
    private readonly IProductIdResolver _productIdResolver;

    public ProductMiddleware(
        ILogger<ProductMiddleware<TUser, TProduct>> logger,
        IProductContextFactory<TProduct> productContextFactory, 
        IProductContextAccessor<TProduct> productContextAccessor, 
        IProductIdResolver productIdResolver)
    {
        _logger = logger;
        _productContextFactory = productContextFactory;
        _productContextAccessor = productContextAccessor;
        _productIdResolver = productIdResolver;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogDebug("Executing Product Middleware...");
        
        if (_productContextAccessor.ProductContext is not null)
        {
            _logger.LogDebug("Product Context already present - Product middleware complete.");
            await next(httpContext);
            return;
        }

        Guid? selectedProductId = null;
        
        (await _productIdResolver.ResolveAsync())
            .Match(
                productId =>
                {
                    _logger.LogDebug("Successfully resolved Product Id {productId}.", productId);
                    selectedProductId = productId;
                },
                () => 
                {
                    _logger.LogDebug("Failed to resolve Product Id.");
                });

        _productContextAccessor.ProductContext = await _productContextFactory.CreateAsync(selectedProductId, CancellationToken.None);
        
        _logger.LogDebug("Product middleware complete.");
        await next(httpContext);
    }
}