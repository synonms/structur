using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Server.Versioning.Faults;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Employees.Projections;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Events;

public class EmployeeCreatedEvent : AggregateCreatedDomainEvent<Employee, EmployeeResource>
{
    private readonly IUserActionProvider _userActionProvider;
    private readonly IVersionContext _versionContext;

    public EmployeeCreatedEvent(IUserActionProvider userActionProvider, IVersionContext versionContext, EntityId<Employee> aggregateId, EmployeeResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
        _userActionProvider = userActionProvider;
        _versionContext = versionContext;
    }

    public override Result<Employee> CreateAggregate(EmployeeResource resource)
    {
        Version? applicableVersion = resource.GetApplicableVersion(_versionContext.Version);

        if (applicableVersion is null)
        {
            return new ApplicableVersionFault(typeof(EmployeeResource));
        }
        
        return Employee.Create(TenantId, resource, _userActionProvider.Get(), applicableVersion);
    }

    public override void Replay(Projection projection)
    {
        if (projection is EmployeeSummaryProjection employeeSummaryProjection)
        {
            employeeSummaryProjection.FullName = Resource.Forename + " " + (string.IsNullOrWhiteSpace(Resource.MiddleNames) ? string.Empty : Resource.MiddleNames + " ") + Resource.Surname;
        }
    }
}