namespace Synonms.Structur.Application.Products;

public abstract class StructurProduct
{
    public Guid Id { get; set; }
}

public sealed class NoStructurProduct : StructurProduct
{
    public NoStructurProduct()
    {
        Id = Guid.Empty;
    }
}