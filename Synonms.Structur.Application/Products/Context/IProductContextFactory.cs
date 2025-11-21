namespace Synonms.Structur.Application.Products.Context;

public interface IProductContextFactory<TProduct>
    where TProduct : StructurProduct
{
    Task<ProductContext<TProduct>> CreateAsync(Guid? selectedProductId, CancellationToken cancellationToken);
}