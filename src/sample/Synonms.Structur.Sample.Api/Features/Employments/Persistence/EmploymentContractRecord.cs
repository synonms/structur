using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Infrastructure.Persistence;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments.Persistence;

public class EmploymentContractRecord : AggregateMemberRecord<EmploymentContract>
{
    public EffectiveDate? StartDate { get; set; }

    public EventDate? ProbationEndDate { get; set; }

    public EffectiveDate? EndDate { get; set; }

    public Period? EmployerNoticePeriod { get; set; }

    public Period? EmployeeNoticePeriod { get; set; }

    public Role? Position { get; set; }

    public WorkLocation? Location { get; set; }

    public Notes? LocationNotes { get; set; }

    public EntityId<Employee>? ReportsToEmployeeId { get; set; }

    public CarRegistrationPlate? CarRegistrationPlate { get; set; }

    public Notes? Notes { get; set; }

    public bool? CanClaimTravelExpensesToOffice { get; set; } = false;
}
