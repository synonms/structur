namespace Synonms.Structur.Application.Tenants.Context;

public class TenantContext
{
    protected TenantContext(StructurTenant? selectedTenant)
    {
        BaseSelectedTenant = selectedTenant;
    }

    public StructurTenant? BaseSelectedTenant { get; }
}

public class TenantContext<TTenant> : TenantContext
    where TTenant : StructurTenant
{
    private TenantContext(IEnumerable<TTenant> availableTenants, TTenant? selectedTenant) : base(selectedTenant)
    {
        AvailableTenants = availableTenants;
        SelectedTenant = selectedTenant;
    }

    public IEnumerable<TTenant> AvailableTenants { get; set; }
    
    public TTenant? SelectedTenant { get; }

    public static TenantContext<TTenant> Create(IEnumerable<TTenant> availableTenants, TTenant? selectedTenant) =>
        new (availableTenants, selectedTenant);
}