using Synonms.Structur.Application.Tenants.Context;
using Synonms.Structur.Application.Users;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualDomainEventFactory : IDomainEventFactory<Individual, IndividualResource>
{
    private readonly ITenantContext<SampleTenant> _tenantContext;
    private readonly IUserActionProvider _userActionProvider;

    public IndividualDomainEventFactory(ITenantContext<SampleTenant> tenantContext, IUserActionProvider userActionProvider)
    {
        _tenantContext = tenantContext;
        _userActionProvider = userActionProvider;
    }
    
    public Result<DomainEvent<Individual>> GenerateCreatedEvent(IndividualResource resource) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Individual>>.Success(new IndividualCreatedEvent(_userActionProvider, (EntityId<Individual>)resource.Id, resource, tenant.Id)));

    public Result<DomainEvent<Individual>> GenerateDeletedEvent(EntityId<Individual> aggregateId) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Individual>>.Success(new IndividualDeletedEvent(_userActionProvider, aggregateId, tenant.Id)));

    public Result<DomainEvent<Individual>> GenerateUpdatedEvent(IndividualResource resource) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Individual>>.Success(new IndividualUpdatedEvent(_userActionProvider, (EntityId<Individual>)Guid.NewGuid(), resource, tenant.Id)));
}