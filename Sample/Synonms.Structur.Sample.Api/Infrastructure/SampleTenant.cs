using Synonms.Structur.Application.Tenants;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public class SampleTenant : StructurTenant
{
    public required string Name { get; set; }
}