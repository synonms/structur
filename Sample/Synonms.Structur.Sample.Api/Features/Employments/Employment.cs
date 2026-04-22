using Synonms.Structur.Api.Server.Domain;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.ClientApi.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments;

[StructurResource(typeof(EmploymentResource), "employments", allowAnonymous: true, isDeleteDisabled: true, pageLimit: 10)]
public class Employment : AggregateRoot<Employment>
{
    private Employment()
    {
    }

    private Employment(
        EntityId<Employment> id,
        Guid tenantId,
        UserAction createdAction,
        EntityId<Employee> employeeId,
        ExternalReference employeeNumber,
        EffectiveDate continuousStartDate,
        List<EmploymentContract> contracts,
        UkBankDetails bankDetails)
        : base(id, tenantId, createdAction)
    {
        EmployeeId = employeeId;
        EmployeeNumber = employeeNumber;
        ContinuousStartDate = continuousStartDate;
        Contracts = contracts;
        BankDetails = bankDetails;
    }

    public EntityId<Employee> EmployeeId { get; private set; } = null!;

    public ExternalReference EmployeeNumber { get; private set; } = null!;

    public EffectiveDate ContinuousStartDate { get; private set; } = null!;

    public List<EmploymentContract> Contracts { get; private set; } = [];

    public UkBankDetails BankDetails { get; private set; } = null!;

    internal Maybe<Fault> Update(EmploymentResource resource, UserActionDto updatedActionDto, Version? applicableVersion = null) =>
        Validator.CreateBuilder<Employment>()
            .WithMandatoryScalarProperty(updatedActionDto, x => UserAction.CreateMandatory(nameof(UpdatedAction), x), out UserAction updatedActionValueObject)
            .WithMandatoryScalarProperty(resource.ContinuousStartDate, x => EffectiveDate.CreateMandatory(nameof(ContinuousStartDate), x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate continuousStartDateValueObject)
            .WithMandatoryScalarProperty(resource.BankDetails, x => UkBankDetails.CreateMandatory(nameof(BankDetails), x.BankName, x.SortCode, x.AccountNumber, x.AccountName, x.BuildingSocietyRollNumber), out UkBankDetails bankDetailsValueObject)
            .Build()
            .BiBind(() => 
                Contracts.MergeChanges(resource.Contracts, r => EmploymentContract.Create(nameof(Contracts), r), (am, r) => am.Update(r, () => MarkAsUpdated(updatedActionValueObject)))
                    .BiBind(() =>
                    {
                        UpdateMandatoryValue(x => x.ContinuousStartDate, continuousStartDateValueObject, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.BankDetails, bankDetailsValueObject, updatedActionValueObject);

                        return Maybe<Fault>.None;
                    }));

    internal static Result<Employment> Create(Guid tenantId, EmploymentResource resource, UserActionDto createdActionDto, Version? applicableVersion = null) =>
        Validator.CreateBuilder<Employment>()
            .WithMandatoryScalarProperty(createdActionDto, x => UserAction.CreateMandatory(nameof(CreatedAction), x), out UserAction createdActionValueObject)
            .WithMandatoryScalarProperty(resource.EmployeeNumber, x => ExternalReference.CreateMandatory(nameof(EmployeeNumber), x), out ExternalReference employeeNumberValueObject)
            .WithMandatoryScalarProperty(resource.ContinuousStartDate, x => EffectiveDate.CreateMandatory(nameof(ContinuousStartDate), x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate continuousStartDateValueObject)
            .WithCollectionProperty(resource.Contracts, x => EmploymentContract.Create(nameof(Contracts), x), out List<EmploymentContract> contractValueObjects)
            .WithMandatoryScalarProperty(resource.BankDetails, x => UkBankDetails.CreateMandatory(nameof(BankDetails), x.BankName, x.SortCode, x.AccountNumber, x.AccountName, x.BuildingSocietyRollNumber), out UkBankDetails bankDetailsValueObject)
            .Build()
            .ToResult(() =>
                new Employment(
                    (EntityId<Employment>)resource.Id,
                    tenantId,
                    createdActionValueObject,
                    (EntityId<Employee>)resource.EmployeeId,
                    employeeNumberValueObject,
                    continuousStartDateValueObject,
                    contractValueObjects,
                    bankDetailsValueObject));
}


