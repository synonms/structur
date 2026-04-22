using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Sample.ClientApi.Features.Employments;

public class EmploymentContractResource : ChildResource
{
    public EmploymentContractResource()
    {
    }

    public EmploymentContractResource(Guid id)
        : base(id)
    {
    }

    [StructurRequired]
    public DateOnly StartDate { get; set; }

    public DateOnly? ProbationEndDate { get; set; }

    public DateOnly? EndDate { get; set; }

    [StructurRequired]
    public PeriodResource EmployerNoticePeriod { get; set; } = new();

    [StructurRequired]
    public PeriodResource EmployeeNoticePeriod { get; set; } = new();

    [StructurRequired]
    public string Position { get; set; } = string.Empty;

    [StructurRequired]
    public string Location { get; set; } = string.Empty;

    public string? LocationNotes { get; set; }

    public Guid? ReportsToEmployeeId { get; set; }

    public string? CarRegistrationPlate { get; set; }

    public string? Notes { get; set; }

    [StructurRequired]
    public bool CanClaimTravelExpensesToOffice { get; set; }
}


