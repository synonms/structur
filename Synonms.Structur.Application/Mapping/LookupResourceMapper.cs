using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Lookups;

namespace Synonms.Structur.Application.Mapping;

public static class LookupResourceMapper
{
    public static LookupResource Map(Lookup lookup) =>
        new()
        {
            Id = lookup.Id.Value,
            LookupCode = lookup.LookupCode,
            LookupName = lookup.LookupName,
        };
    
    public static LookupResource? MapOptional(Lookup? lookup) =>
        lookup is null
            ? null
            : new LookupResource
            {
                Id = lookup.Id.Value,
                LookupCode = lookup.LookupCode,
                LookupName = lookup.LookupName,
            };
}