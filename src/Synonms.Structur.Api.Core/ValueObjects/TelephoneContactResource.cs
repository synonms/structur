using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Api.Core.ValueObjects;

public class TelephoneContactResource : ComplexValueObjectResource
{
    [StructurRequired]
    public TelephoneContactTypeEnumeration Type { get; set; }
    
    [StructurRequired]
    [StructurPattern(RegularExpressions.TelephoneNumber)]
    public string Number { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = true;
    
    public string? Label { get; set; }
}