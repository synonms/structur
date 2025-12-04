using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Products.Faults;

public class ProductContextFault : ApplicationFault
{
    public ProductContextFault()
        : base(nameof(ProductContextFault), "Product Context", "Product Context not set - check the Product middleware.", new FaultSource())
    {
    }
}