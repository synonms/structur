using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Api.Server.Products.Faults;

public class ProductContextFault : ApplicationFault
{
    public ProductContextFault()
        : base(nameof(ProductContextFault), "Product Context", "Product Context not set - check the Product middleware.", new FaultSource())
    {
    }
}