using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Sample.Api.Features.Employees.Projections;

[StructurProjection(typeof(Employee), "summary", "Employee Summary", "A high level summary of the Employee.", allowAnonymous: true)]
public class EmployeeSummaryProjection : Projection<Employee>
{
    public string FullName { get; set; } = string.Empty;
}