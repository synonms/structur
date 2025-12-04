using Synonms.Structur.Application.Products;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public class SampleProduct : StructurProduct
{
    public required string Name { get; set; }
}