namespace Synonms.Structur.Application.Tenants;

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