using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Sample.Api.Features.Employees.Events;

public class EmployeeDeletedEvent : AggregateDeletedDomainEvent<Employee>
{
    private readonly IUserActionProvider _userActionProvider;

    public EmployeeDeletedEvent(IUserActionProvider userActionProvider, EntityId<Employee> aggregateId, Guid tenantId) : base(aggregateId, tenantId)
    {
        _userActionProvider = userActionProvider;
    }

    public override void Replay(Projection projection)
    {
    }

    public override Result<Employee> DeleteAggregate(Employee aggregateRoot) =>
        aggregateRoot.Delete(_userActionProvider.Get()).ToResult(() => aggregateRoot);
}