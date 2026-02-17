using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Core.Entities;

namespace Synonms.Structur.Sample.Api.Features.Employees.Domain.Events;

public class EmployeeDomainEventFactory : IDomainEventFactory<Employee, EmployeeResource>
{
    private readonly ITenantContext<SampleTenant> _tenantContext;
    private readonly IUserActionProvider _userActionProvider;

    public EmployeeDomainEventFactory(ITenantContext<SampleTenant> tenantContext, IUserActionProvider userActionProvider)
    {
        _tenantContext = tenantContext;
        _userActionProvider = userActionProvider;
    }
    
    public Result<DomainEvent<Employee>> GenerateCreatedEvent(EmployeeResource resource) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employee>>.Success(new EmployeeCreatedEvent(_userActionProvider, (EntityId<Employee>)resource.Id, resource, tenant.Id)));

    public Result<DomainEvent<Employee>> GenerateDeletedEvent(EntityId<Employee> aggregateId) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employee>>.Success(new EmployeeDeletedEvent(_userActionProvider, aggregateId, tenant.Id)));

    public Result<DomainEvent<Employee>> GenerateUpdatedEvent(EmployeeResource resource) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employee>>.Success(new EmployeeUpdatedEvent(_userActionProvider, (EntityId<Employee>)Guid.NewGuid(), resource, tenant.Id)));
}