using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Tenants.Context;

public interface ITenantContext
{
    Guid? GetTenantId();
    
    bool HasTenantId();
}

public interface ITenantContext<TTenant> : ITenantContext where TTenant : StructurTenant
{
    Result<TTenant> GetTenant();

    bool HasTenant();
    
    Task SelectTenantAsync(Guid? tenantId, CancellationToken cancellationToken = default);
}