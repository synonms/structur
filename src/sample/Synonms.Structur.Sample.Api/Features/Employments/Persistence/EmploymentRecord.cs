using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Infrastructure.Persistence;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments.Persistence;

public class EmploymentRecord : AggregateRootRecord<Employment>
{
    public EntityId<Employee>? EmployeeId { get; set; }

    public UniqueReference? EmploymentReference { get; set; }

    public EffectiveDate? ContinuousStartDate { get; set; }

    public List<EmploymentContractRecord>? Contracts { get; set; } = [];

    public UkBankDetails? BankDetails { get; set; }
}
