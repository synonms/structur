using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Infrastructure.Persistence;
using Synonms.Structur.Sample.Api.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Persistence;

public class EmployeeEqualOpportunitiesRecord : AggregateMemberRecord<EmployeeEqualOpportunities>
{
    public EventDate? BirthDate { get; set; }

    public Sex? Sex { get; set; }
}
