using Synonms.Structur.Application.Users;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;

public class IndividualDeletedEvent : AggregateDeletedDomainEvent<Individual>
{
    private readonly IUserActionProvider _userActionProvider;

    public IndividualDeletedEvent(IUserActionProvider userActionProvider, EntityId<Individual> aggregateId, Guid tenantId) : base(aggregateId, tenantId)
    {
        _userActionProvider = userActionProvider;
    }

    public override void Replay(Projection projection)
    {
    }

    public override Result<Individual> DeleteAggregate(Individual aggregateRoot) =>
        aggregateRoot.Delete(_userActionProvider.Get()).ToResult(() => aggregateRoot);
}