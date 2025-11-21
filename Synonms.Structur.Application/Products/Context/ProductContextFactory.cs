using Synonms.Structur.Application.Products.Persistence;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Context;

public class ProductContextFactory<TProduct> : IProductContextFactory<TProduct>
    where TProduct : StructurProduct
{
    private readonly IProductRepository<TProduct> _repository;

    public ProductContextFactory(IProductRepository<TProduct> repository)
    {
        _repository = repository;
    }
        
    public async Task<ProductContext<TProduct>> CreateAsync(Guid? selectedProductId, CancellationToken cancellationToken)
    {
        TProduct? selectedProduct = null;
        
        if (selectedProductId is not null)
        {
            await _repository.FindSelectedProductAsync(selectedProductId.Value, cancellationToken)
                .IfSomeAsync(product => selectedProduct = product);
        }

        IEnumerable<TProduct> availableProducts = await _repository.ReadAvailableProductsAsync(cancellationToken);

        return ProductContext<TProduct>.Create(availableProducts, selectedProduct);
    }
}