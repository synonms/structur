namespace Synonms.Structur.Sample.Ui.Infrastructure;

public class TenantContextAccessor
{
    public Guid SelectedTenantId { get; set; } = Tenants.LaLakers.Id;
    public string SelectedTenantName { get; set; } = Tenants.LaLakers.Name;

    public void SetLaLakers()
    {
        SelectedTenantId = Tenants.LaLakers.Id;
        SelectedTenantName = Tenants.LaLakers.Name;
    }

    public void SetTottenhamHotspur()
    {
        SelectedTenantId = Tenants.TottenhamHotspur.Id;
        SelectedTenantName = Tenants.TottenhamHotspur.Name;
    }
}