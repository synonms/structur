using Synonms.Structur.Application.Tenants.Context;
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

    public IndividualDomainEventFactory(ITenantContext<SampleTenant> tenantContext)
    {
        _tenantContext = tenantContext;
    }
    
    public Result<DomainEvent<Individual>> GenerateCreatedEvent(IndividualResource resource) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Individual>>.Success(new IndividualCreatedEvent((EntityId<Individual>)resource.Id, resource, tenant.Id)));

    public Result<DomainEvent<Individual>> GenerateDeletedEvent(EntityId<Individual> aggregateId) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Individual>>.Success(new IndividualDeletedEvent(aggregateId, tenant.Id)));

    public Result<DomainEvent<Individual>> GenerateUpdatedEvent(IndividualResource resource) =>
        _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Individual>>.Success(new IndividualUpdatedEvent((EntityId<Individual>)Guid.NewGuid(), resource, tenant.Id)));
}