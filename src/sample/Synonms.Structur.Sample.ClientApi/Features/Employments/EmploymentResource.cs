using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Sample.ClientApi.Features.Employments;

public class EmploymentResource : Resource
{
    public const string CollectionPath = "employments";

    public EmploymentResource()
    {
    }

    public EmploymentResource(Guid id, Link selfLink)
        : base(id, selfLink)
    {
    }

    public override string GetCollectionPath() => CollectionPath;

    public override SortedSet<Version> SupportedVersions { get; } = 
    [
        new Version(1, 0)
    ];

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
