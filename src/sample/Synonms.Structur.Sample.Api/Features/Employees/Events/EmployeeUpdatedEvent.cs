using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Server.Versioning.Faults;
using Synonms.Structur.Api.Server.Versioning;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Sample.Api.Features.Employees.Projections;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Events;

public class EmployeeUpdatedEvent: AggregateUpdatedDomainEvent<Employee, EmployeeResource>
{
    private readonly IUserActionProvider _userActionProvider;
    private readonly IVersionContext _versionContext;

    public EmployeeUpdatedEvent(IUserActionProvider userActionProvider, IVersionContext versionContext, EntityId<Employee> aggregateId, ResourceEnvelope<EmployeeResource> envelope, Guid tenantId) : base(aggregateId, envelope, tenantId)
    {
        _userActionProvider = userActionProvider;
        _versionContext = versionContext;
    }

    public override Maybe<Fault> UpdateAggregate(Employee aggregateRoot, ResourceEnvelope<EmployeeResource> envelope)
    {
        Version? applicableVersion = ResourceVersionResolver.Resolve(typeof(Employee), typeof(EmployeeResource), _versionContext.Version);

        if (applicableVersion is null)
        {
            return new ApplicableVersionFault(typeof(EmployeeResource));
        }

        return aggregateRoot.Update(envelope, _userActionProvider.Get(), applicableVersion);
    }

    public override void Replay(Projection projection)
    {
        if (projection is EmployeeSummaryProjection employeeSummaryProjection)
        {
            EmployeeResource resource = Envelope.Data;
            employeeSummaryProjection.FullName = resource.Forename + " " + (string.IsNullOrWhiteSpace(resource.MiddleNames) ? string.Empty : resource.MiddleNames + " ") + resource.Surname;
        }
    }
}