using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Api.Core.ValueObjects;

public class AddressValueObjectResource : ComplexValueObjectResource
{
    [StructurRequired]
    public AddressTypeEnumeration Type { get; set; }

    [StructurRequired]
    [StructurPattern(RegularExpressions.AddressLine)]
    public string Line1 { get; set; } = string.Empty;

    [StructurPattern(RegularExpressions.AddressLine)]
    public string? Line2 { get; set; }

    [StructurPattern(RegularExpressions.AddressLine)]
    public string? Line3 { get; set; }

    [StructurPattern(RegularExpressions.AddressLine)]
    public string? Line4 { get; set; }

    [StructurRequired]
    [StructurPattern(RegularExpressions.Postcode)]
    public string Postcode { get; set; } = string.Empty;
    
    public string? Label { get; set; }
}