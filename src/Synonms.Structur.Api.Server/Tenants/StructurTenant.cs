namespace Synonms.Structur.Api.Server.Tenants;

public abstract class StructurTenant
{
    public Guid Id { get; set; }
}

public sealed class NoStructurTenant : StructurTenant
{
    public NoStructurTenant()
    {
        Id = Guid.Empty;
    }
}