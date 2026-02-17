using Synonms.Structur.Api.Server.Tenants;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public class SampleTenant : StructurTenant
{
    public required string Name { get; set; }
}