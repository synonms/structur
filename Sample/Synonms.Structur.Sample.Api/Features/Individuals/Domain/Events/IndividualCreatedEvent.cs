using Synonms.Structur.Application.Users;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualCreatedEvent : AggregateCreatedDomainEvent<Individual, IndividualResource>
{
    private readonly IUserActionProvider _userActionProvider;

    public IndividualCreatedEvent(IUserActionProvider userActionProvider, EntityId<Individual> aggregateId, IndividualResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
        _userActionProvider = userActionProvider;
    }
    
    public override Result<Individual> CreateAggregate(IndividualResource resource) => 
        Individual.Create(TenantId, resource, _userActionProvider.Get());

    public override void Replay(Projection projection)
    {
    }
}