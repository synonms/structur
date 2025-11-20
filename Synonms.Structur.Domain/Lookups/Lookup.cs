using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Domain.Lookups;

public class Lookup : Entity<Lookup>
{
    public string Discriminator { get; init; } = string.Empty;
    
    public string LookupCode { get; init; } = string.Empty;
    
    public string LookupName { get; init; } = string.Empty;
}