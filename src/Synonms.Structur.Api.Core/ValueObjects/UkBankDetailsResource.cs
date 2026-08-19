using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Api.Core.ValueObjects;

public class UkBankDetailsResource : ComplexValueObjectResource
{
    [StructurRequired]
    public string BankName { get; set; } = string.Empty;

    [StructurRequired]
    public string SortCode { get; set; } = string.Empty;

    [StructurRequired]
    public string AccountNumber { get; set; } = string.Empty;

    [StructurRequired]
    public string AccountName { get; set; } = string.Empty;

    public string? BuildingSocietyRollNumber { get; set; }
}
