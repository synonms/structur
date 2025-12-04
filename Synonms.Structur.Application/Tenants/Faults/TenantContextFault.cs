using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Tenants.Faults;

public class TenantContextFault : ApplicationFault
{
    public TenantContextFault()
        : base(nameof(TenantContextFault), "Tenant Context", "Tenant Context not set - check the Tenant middleware.", new FaultSource())
    {
    }
}