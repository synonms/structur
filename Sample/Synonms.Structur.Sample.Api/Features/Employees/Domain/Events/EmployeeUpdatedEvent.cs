using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Core.Entities;

namespace Synonms.Structur.Sample.Api.Features.Employees.Domain.Events;

public class EmployeeUpdatedEvent: AggregateUpdatedDomainEvent<Employee, EmployeeResource>
{
    private readonly IUserActionProvider _userActionProvider;

    public EmployeeUpdatedEvent(IUserActionProvider userActionProvider, EntityId<Employee> aggregateId, EmployeeResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
        _userActionProvider = userActionProvider;
    }

    public override Maybe<Fault> UpdateAggregate(Employee aggregateRoot, EmployeeResource resource) =>
        aggregateRoot.Update(resource, _userActionProvider.Get());

    public override void Replay(Projection projection)
    {
    }
}