using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Application.Products;
using Synonms.Structur.Application.Products.Context;
using Synonms.Structur.Application.Products.Resolution;
using Synonms.Structur.Application.Users;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.WebApi.Products;

public class ProductMiddleware<TUser, TProduct> : IMiddleware
    where TUser : StructurUser
    where TProduct : StructurProduct
{
    private readonly ILogger<ProductMiddleware<TUser, TProduct>> _logger;
    private readonly IProductContext<TProduct> _productContext;
    private readonly IProductIdResolver _productIdResolver;

    public ProductMiddleware(
        ILogger<ProductMiddleware<TUser, TProduct>> logger,
        IProductContext<TProduct> productContext, 
        IProductIdResolver productIdResolver)
    {
        _logger = logger;
        _productContext = productContext;
        _productIdResolver = productIdResolver;
    }
    
    public async Task InvokeAsync(HttpContext httpContext, RequestDelegate next)
    {
        _logger.LogTrace("{ClassName}.{FunctionName}", nameof(ProductMiddleware<TUser, TProduct>), nameof(InvokeAsync));
        
        if (typeof(TProduct) == typeof(NoStructurProduct))
        {
            _logger.LogDebug("No Product required - Product middleware complete.");
            await next(httpContext);
            return;
        }
        
        if (_productContext.HasProduct())
        {
            _logger.LogDebug("Product already present - Product middleware complete.");
            await next(httpContext);
            return;
        }
        
        await _productIdResolver.ResolveAsync()
            .MatchAsync(
                async productId =>
                {
                    _logger.LogInformation("Successfully determined Product Id {ProductId} from request.", productId);
                    
                    await _productContext.SelectProductAsync(productId, CancellationToken.None);

                    if (_productContext.HasProduct())
                    {
                        _logger.LogInformation("Successfully resolved Product Id {ProductId}.", productId);
                    }
                    else
                    {
                        _logger.LogWarning("Unable to resolve Product Id {ProductId}.", productId);
                    }
                },
                () => _logger.LogWarning("Failed to determine Product Id from request."));

        await next(httpContext);
    }
}