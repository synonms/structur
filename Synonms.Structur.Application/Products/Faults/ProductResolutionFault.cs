using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Products.Faults;

public class ProductResolutionFault : ApplicationFault
{
    public ProductResolutionFault(Guid productId)
        : base(nameof(ProductResolutionFault), "Product Resolution", "Unable to resolve Product Id '{ProductId}'.", new FaultSource(), productId)
    {
    }
}