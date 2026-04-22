using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Server.Versioning.Faults;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Employments.Projections;
using Synonms.Structur.Sample.ClientApi.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments.Events;

public class EmploymentUpdatedEvent : AggregateUpdatedDomainEvent<Employment, EmploymentResource>
{
    private readonly IUserActionProvider _userActionProvider;
    private readonly IVersionContext _versionContext;

    public EmploymentUpdatedEvent(IUserActionProvider userActionProvider, IVersionContext versionContext, EntityId<Employment> aggregateId, EmploymentResource resource, Guid tenantId) : base(aggregateId, resource, tenantId)
    {
        _userActionProvider = userActionProvider;
        _versionContext = versionContext;
    }

    public override Maybe<Fault> UpdateAggregate(Employment aggregateRoot, EmploymentResource resource)
    {
        Version? applicableVersion = resource.GetApplicableVersion(_versionContext.Version);

        if (applicableVersion is null)
        {
            return new ApplicableVersionFault(typeof(EmploymentResource));
        }

        return aggregateRoot.Update(resource, _userActionProvider.Get(), applicableVersion);
    }

    public override void Replay(Projection projection)
    {
        if (projection is EmploymentSummaryProjection employmentSummaryProjection)
        {
            employmentSummaryProjection.EmployeeNumber = Resource.EmployeeNumber;
            employmentSummaryProjection.ContinuousStartDate = Resource.ContinuousStartDate;
        }
    }
}

