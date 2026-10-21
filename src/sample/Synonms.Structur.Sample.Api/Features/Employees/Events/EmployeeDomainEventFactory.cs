using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Events;

public class EmployeeDomainEventFactory : IDomainEventFactory<Employee, EmployeeResource>
{
    private readonly ITenantContext<SampleTenant> _tenantContext;
    private readonly IVersionContext _versionContext;
    private readonly IUserActionProvider _userActionProvider;
    private readonly IReadAggregateRepository<Employee> _readEmployeeRepository;

    public EmployeeDomainEventFactory(ITenantContext<SampleTenant> tenantContext, IVersionContext versionContext, IUserActionProvider userActionProvider, IReadAggregateRepository<Employee> readEmployeeRepository)
    {
        _tenantContext = tenantContext;
        _versionContext = versionContext;
        _userActionProvider = userActionProvider;
        _readEmployeeRepository = readEmployeeRepository;
    }

    public async Task<Result<DomainEvent<Employee>>> GenerateCreatedEvent(ResourceEnvelope<EmployeeResource> envelope, CancellationToken cancellationToken)
    {
        EmployeeResource resource = envelope.Data;
        string nationalInsuranceNumber = resource.NationalInsuranceNumber;
        AggregateQuery<Employee> duplicateNumberQuery = AggregateQuery<Employee>.All
            .Where(x => x.NationalInsuranceNumber.Value == nationalInsuranceNumber);
        bool isNiNumberAlreadyPresent = await _readEmployeeRepository.AnyAsync(duplicateNumberQuery, cancellationToken);

        if (isNiNumberAlreadyPresent)
        {
            return Result<DomainEvent<Employee>>.Failure(new DomainRuleFault("National Insurance number is already present in the system.", new FaultSource(nameof(EmployeeResource.NationalInsuranceNumber), resource.NationalInsuranceNumber)));
        }
        
        return _tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employee>>.Success(new EmployeeCreatedEvent(_userActionProvider, _versionContext, envelope, tenant.Id)));
    }

    public Task<Result<DomainEvent<Employee>>> GenerateDeletedEvent(EntityId<Employee> aggregateId, CancellationToken cancellationToken) =>
        Task.FromResult(_tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employee>>.Success(new EmployeeDeletedEvent(_userActionProvider, aggregateId, tenant.Id))));

    public Task<Result<DomainEvent<Employee>>> GenerateUpdatedEvent(ResourceEnvelope<EmployeeResource> envelope, CancellationToken cancellationToken) =>
        Task.FromResult(_tenantContext.GetTenant().Bind(tenant => Result<DomainEvent<Employee>>.Success(new EmployeeUpdatedEvent(_userActionProvider, _versionContext, (EntityId<Employee>)envelope.Metadata.Id, envelope, tenant.Id))));
}