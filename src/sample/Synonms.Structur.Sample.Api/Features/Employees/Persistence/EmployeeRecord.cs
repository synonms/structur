using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Infrastructure.Persistence;
using Synonms.Structur.Sample.Api.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Persistence;

public class EmployeeRecord : AggregateRootRecord<Employee>
{
    public UniqueReference? EmployeeReference { get; set; }

    public NationalInsuranceNumber? NationalInsuranceNumber { get; set; }

    public Title? Title { get; set; }

    public Moniker? Forename { get; set; }

    public Moniker? MiddleNames { get; set; }

    public Moniker? Surname { get; set; }

    public Moniker? KnownAs { get; set; }

    public bool? WorkPermitRequired { get; set; } = false;

    public EffectiveDate? WorkPermitValidUntil { get; set; }

    public Notes? Notes { get; set; }

    public Address? HomeAddress { get; set; }

    public List<EmailContact>? EmailContacts { get; set; } = [];

    public List<TelephoneContact>? TelephoneContacts { get; set; } = [];

    public EmployeeEqualOpportunitiesRecord? EqualOpportunities { get; set; }
}
