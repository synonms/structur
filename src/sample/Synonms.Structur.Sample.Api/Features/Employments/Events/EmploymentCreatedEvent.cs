using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Server.Versioning.Faults;
using Synonms.Structur.Api.Server.Versioning;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Employments.Projections;
using Synonms.Structur.Sample.ClientApi.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments.Events;

public class EmploymentCreatedEvent : AggregateCreatedDomainEvent<Employment, EmploymentResource>
{
    private readonly IUserActionProvider _userActionProvider;
    private readonly IVersionContext _versionContext;

    public EmploymentCreatedEvent(IUserActionProvider userActionProvider, IVersionContext versionContext, ResourceEnvelope<EmploymentResource> envelope, Guid tenantId) : base(envelope, tenantId)
    {
        _userActionProvider = userActionProvider;
        _versionContext = versionContext;
    }

    public override Result<Employment> CreateAggregate(ResourceEnvelope<EmploymentResource> envelope)
    {
        Version? applicableVersion = ResourceVersionResolver.Resolve(typeof(Employment), typeof(EmploymentResource), _versionContext.Version);

        if (applicableVersion is null)
        {
            return new ApplicableVersionFault(typeof(EmploymentResource));
        }

        return Employment.Create(TenantId, envelope, _userActionProvider.Get(), applicableVersion);
    }

    public override void Replay(Projection projection)
    {
        if (projection is EmploymentSummaryProjection employmentSummaryProjection)
        {
            employmentSummaryProjection.EmployeeNumber = Envelope.Data.EmploymentReference;
            employmentSummaryProjection.ContinuousStartDate = Envelope.Data.ContinuousStartDate;
        }
    }
}
