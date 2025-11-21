using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Persistence;

public interface IProductRepository<TProduct>
    where TProduct : StructurProduct
{
    Task<IEnumerable<TProduct>> ReadAvailableProductsAsync(CancellationToken cancellationToken);
    
    Task<Maybe<TProduct>> FindSelectedProductAsync(Guid id, CancellationToken cancellationToken);
}