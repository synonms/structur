using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Tenants.Faults;

public class TenantResolutionFault : ApplicationFault
{
    public TenantResolutionFault(Guid tenantId)
        : base(nameof(TenantResolutionFault), "Tenant Resolution", "Unable to resolve Tenant Id '{TenantId}'.", new FaultSource(), tenantId)
    {
    }
}