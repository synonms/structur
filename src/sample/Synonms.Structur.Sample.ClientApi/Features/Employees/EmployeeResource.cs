
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Sample.ClientApi.Features.Employees;

public class EmployeeResource : Resource
{
    public const string CollectionPath = "employees";

    public EmployeeResource()
    {
    }

    public EmployeeResource(Guid id, Link selfLink)
        : base(id, selfLink)
    {
    }

    public override string GetCollectionPath() => CollectionPath;
    
    public override SortedSet<Version> SupportedVersions { get; } = 
    [
        new Version(1, 0),
        new Version(1, 1)
    ];
    
    [StructurRequired]
    [StructurImmutable]
    public string EmployeeReference { get; set; } = string.Empty;

    [StructurRequired]
    [StructurImmutable]
    [StructurPattern(RegularExpressions.NationalInsuranceNumber)]
    public string NationalInsuranceNumber { get; set; } = string.Empty;

    public TitleEnumeration? Title { get; set; }
    
    [StructurRequired]
    [StructurMaxLength(100)]
    public string Forename { get; set; } = string.Empty;

    public string? MiddleNames { get; set; }

    [StructurRequired]
    [StructurMaxLength(100)]
    public string Surname { get; set; } = string.Empty;

    public string? KnownAs { get; set; }

    [StructurRequired]
    public bool WorkPermitRequired { get; set; }

    public DateOnly? WorkPermitValidUntil { get; set; }

    public string? Notes { get; set; }

    [StructurRequired]
    public AddressResource HomeAddress { get; set; } = new();

    public List<EmailContactResource> EmailContacts { get; set; } = [];

    public List<TelephoneContactResource> TelephoneContacts { get; set; } = [];
    
    [StructurRequired]
    [StructurVersionHistory(IntroducedMajorVersion = 1, IntroducedMinorVersion = 1)]
    public EmployeeEqualOpportunitiesResource? EqualOpportunities { get; set; }
}