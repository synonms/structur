using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Employees.Projections;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Events;

public class EmployeeCreatedEvent : AggregateCreatedDomainEvent<Employee, EmployeeResource>
{
    private readonly IUserActionProvider _userActionProvider;

    public EmployeeCreatedEvent(IUserActionProvider userActionProvider, EntityId<Employee> aggregateId, EmployeeResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
        _userActionProvider = userActionProvider;
    }
    
    public override Result<Employee> CreateAggregate(EmployeeResource resource) => 
        Employee.Create(TenantId, resource, _userActionProvider.Get());

    public override void Replay(Projection projection)
    {
        if (projection is EmployeeSummaryProjection employeeSummaryProjection)
        {
            employeeSummaryProjection.FullName = Resource.Forename + " " + (string.IsNullOrWhiteSpace(Resource.MiddleNames) ? string.Empty : Resource.MiddleNames + " ") + Resource.Surname;
        }
    }
}