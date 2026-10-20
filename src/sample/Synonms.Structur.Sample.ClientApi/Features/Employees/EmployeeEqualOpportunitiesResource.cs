using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Core.Attributes;

namespace Synonms.Structur.Sample.ClientApi.Features.Employees;

public class EmployeeEqualOpportunitiesResource
{
    [StructurRequired]
    [StructurImmutable]
    public DateOnly BirthDate { get; set; }
    
    [StructurRequired]
    [StructurImmutable]
    public SexEnumeration Sex { get; set; }
}