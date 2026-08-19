using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments.Events;

public class EmploymentDomainEventFactory : IDomainEventFactory<Employment, EmploymentResource>
{
    private readonly ITenantContext<SampleTenant> _tenantContext;
    private readonly IVersionContext _versionContext;
    private readonly IUserActionProvider _userActionProvider;

    public EmploymentDomainEventFactory(ITenantContext<SampleTenant> tenantContext, IVersionContext versionContext, IUserActionProvider userActionProvider)
    {
        _tenantContext = tenantContext;
        _versionContext = versionContext;
        _userActionProvider = userActionProvider;
    }

    public Task<Result<DomainEvent<Employment>>> GenerateCreatedEvent(EmploymentResource resource, CancellationToken cancellationToken) =>
        Task.FromResult(_tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employment>>.Success(new EmploymentCreatedEvent(_userActionProvider, _versionContext, (EntityId<Employment>)resource.Id, resource, tenant.Id))));

    public Task<Result<DomainEvent<Employment>>> GenerateDeletedEvent(EntityId<Employment> aggregateId, CancellationToken cancellationToken) =>
        Task.FromResult(Result<DomainEvent<Employment>>.Failure(new DomainRuleFault("Employment deletion is not supported.")));

    public Task<Result<DomainEvent<Employment>>> GenerateUpdatedEvent(EmploymentResource resource, CancellationToken cancellationToken) =>
        Task.FromResult(_tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employment>>.Success(new EmploymentUpdatedEvent(_userActionProvider, _versionContext, (EntityId<Employment>)resource.Id, resource, tenant.Id))));
}



