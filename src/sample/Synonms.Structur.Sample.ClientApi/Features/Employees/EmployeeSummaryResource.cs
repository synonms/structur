namespace Synonms.Structur.Sample.ClientApi.Features.Employees;

/// <summary>
/// Deliberately restricted, read-only representation of an employee.
/// </summary>
public class EmployeeSummaryResource
{
    public string Forename { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;
}
