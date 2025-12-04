using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Tenants.Faults;

public class TenantIdResolutionFault : ApplicationFault
{
    public TenantIdResolutionFault()
        : base(nameof(TenantIdResolutionFault), "Tenant Id", "Unable to determine Tenant Id from request.", new FaultSource())
    {
    }
}