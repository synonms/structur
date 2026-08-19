using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Core.ValueObjects;

public class PeriodResource : ComplexValueObjectResource
{
    [StructurRequired]
    [StructurMinValue(0)]
    public int Units { get; set; }

    [StructurRequired]
    public string Interval { get; set; } = string.Empty;
}
