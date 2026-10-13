using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Sample.ClientApi.Features.Employments;

public class EmploymentResource
{
    public const string CollectionPath = "employments";

    [StructurRequired]
    [StructurImmutable]
    public Guid EmployeeId { get; set; }

    [StructurRequired]
    [StructurImmutable]
    public string EmploymentReference { get; set; } = string.Empty;

    [StructurRequired]
    public DateOnly ContinuousStartDate { get; set; }

    [StructurRequired]
    public List<EmploymentContractResource> Contracts { get; set; } = [];

    [StructurRequired]
    public UkBankDetailsResource BankDetails { get; set; } = new();
}
