using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Api.Server.Products.Faults;

public class ProductIdResolutionFault : ApplicationFault
{
    public ProductIdResolutionFault()
        : base(nameof(ProductIdResolutionFault), "Product Id", "Unable to determine Product Id from request.", new FaultSource())
    {
    }
}