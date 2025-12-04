using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Products.Faults;

public class ProductTypeFault : ApplicationFault
{
    public ProductTypeFault(Guid productId, Type productType)
        : base(nameof(ProductTypeFault), "Product Type", "Product Id '{ProductId}' is not of expected type {ProductType}.", new FaultSource(), productId, productType.Name)
    {
    }
}