using Synonms.Structur.Application.Products.Faults;
using Synonms.Structur.Application.Products.Persistence;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Context;

public class ProductContext : IProductContext
{
    protected Guid? ProductId;

    public Guid? GetProductId() => ProductId;
    
    public bool HasProductId() => ProductId is not null;
}

public class ProductContext<TProduct> : ProductContext, IProductContext<TProduct> where TProduct : StructurProduct
{
    private readonly IProductRepository<TProduct> _repository;
    private TProduct? _product;

    public ProductContext(IProductRepository<TProduct> repository)
    {
        _repository = repository;
    }
    
    public Result<TProduct> GetProduct() => 
        ProductId is null 
            ? new ProductIdResolutionFault() 
            : _product is null 
                ? new ProductResolutionFault(ProductId.Value) 
                : _product;

    public bool HasProduct() => _product is not null;

    public async Task SelectProductAsync(Guid? productId, CancellationToken cancellationToken = default)
    {
        ProductId = productId;

        if (productId is null)
        {
            _product = null;
            return;
        }
        
        await _repository.FindSelectedProductAsync(productId.Value, cancellationToken)
            .MatchAsync(
                tenant => _product = tenant, 
                () => _product = null);
    }
}