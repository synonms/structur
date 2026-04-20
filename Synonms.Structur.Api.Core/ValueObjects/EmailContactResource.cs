using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Api.Core.ValueObjects;

public class EmailContactResource : ComplexValueObjectResource
{
    [StructurRequired]
    [StructurPattern(RegularExpressions.EmailAddress)]
    public string Address { get; set; } = string.Empty;

    public bool IsPrimary { get; set; } = true;
    
    public string? Label { get; set; }
}
