using Synonms.Structur.Application.Faults;
using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Tenants.Faults;

public class TenantTypeFault : ApplicationFault
{
    public TenantTypeFault(Guid tenantId, Type tenantType)
        : base(nameof(TenantTypeFault), "Tenant Type", "Tenant Id '{TenantId}' is not of expected type {TenantType}.", new FaultSource(), tenantId, tenantType.Name)
    {
    }
}