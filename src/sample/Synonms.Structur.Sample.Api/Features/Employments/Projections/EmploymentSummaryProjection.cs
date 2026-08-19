using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Sample.Api.Features.Employments.Projections;

[StructurProjection(typeof(Employment), "summary", "Employment Summary", "A high level summary of the Employment.", allowAnonymous: true)]
public class EmploymentSummaryProjection : Projection<Employment>
{
    public string EmployeeNumber { get; set; } = string.Empty;
    
    public DateOnly ContinuousStartDate { get; set; }
}
