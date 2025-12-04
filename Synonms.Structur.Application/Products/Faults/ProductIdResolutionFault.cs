using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Products.Faults;

public class ProductIdResolutionFault : ApplicationFault
{
    public ProductIdResolutionFault()
        : base(nameof(ProductIdResolutionFault), "Product Id", "Unable to determine Product Id from request.", new FaultSource())
    {
    }
}