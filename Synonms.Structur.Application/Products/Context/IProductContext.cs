using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Context;

public interface IProductContext
{
    Guid? GetProductId();
    
    bool HasProductId();
}

public interface IProductContext<TProduct> : IProductContext where TProduct : StructurProduct
{
    Result<TProduct> GetProduct();

    bool HasProduct();
    
    Task SelectProductAsync(Guid? productId, CancellationToken cancellationToken = default);
}