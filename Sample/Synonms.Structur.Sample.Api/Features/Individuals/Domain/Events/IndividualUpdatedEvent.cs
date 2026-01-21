using Synonms.Structur.Application.Users;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualUpdatedEvent: AggregateUpdatedDomainEvent<Individual, IndividualResource>
{
    private readonly IUserActionProvider _userActionProvider;

    public IndividualUpdatedEvent(IUserActionProvider userActionProvider, EntityId<Individual> aggregateId, IndividualResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
        _userActionProvider = userActionProvider;
    }

    public override Maybe<Fault> UpdateAggregate(Individual aggregateRoot, IndividualResource resource) =>
        aggregateRoot.Update(resource, _userActionProvider.Get());

    public override void Replay(Projection projection)
    {
    }
}