using Synonms.Structur.Api.Server.Domain;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Persistence;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.ClientApi.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments;

[StructurResource(typeof(EmploymentResource), "employments", allowAnonymous: true, isDeleteDisabled: true, pageLimit: 10, SupportedApiVersions = new[] { "1.0" })]
[GenerateStorage("employment", 1)]
public partial class Employment : AggregateRoot<Employment>
{
    private Employment()
    {
    }

    private Employment(
        EntityId<Employment> id,
        Guid tenantId,
        UserAction createdAction,
        EntityId<Employee> employeeId,
        UniqueReference employmentReference,
        EffectiveDate continuousStartDate,
        List<EmploymentContract> contracts,
        UkBankDetails bankDetails)
        : base(id, tenantId, createdAction)
    {
        EmployeeId = employeeId;
        EmploymentReference = employmentReference;
        ContinuousStartDate = continuousStartDate;
        Contracts = contracts;
        BankDetails = bankDetails;
    }

    [StorageOverride(typeof(Guid), typeof(EmployeeIdStorageCodec))]
    [StoredState("EmployeeId")]
    public EntityId<Employee> EmployeeId { get; private set; } = null!;

    [StoredState("EmploymentReference")]
    public UniqueReference EmploymentReference { get; private set; } = null!;

    [StoredState("ContinuousStartDate")]
    public EffectiveDate ContinuousStartDate { get; private set; } = null!;

    [StoredState("Contracts")]
    public List<EmploymentContract> Contracts { get; private set; } = [];

    [StoredState("BankDetails")]
    public UkBankDetails BankDetails { get; private set; } = null!;

    internal Maybe<Fault> Update(ResourceEnvelope<EmploymentResource> envelope, UserActionDto updatedActionDto, Version? applicableVersion = null)
    {
        EmploymentResource resource = envelope.Data;
        
        return Validator.CreateBuilder<Employment>()
            .WithMandatoryScalarProperty(updatedActionDto, x => UserAction.CreateMandatory(nameof(UpdatedAction), x), out UserAction updatedActionValueObject)
            .WithMandatoryScalarProperty(resource.ContinuousStartDate, x => EffectiveDate.CreateMandatory(nameof(ContinuousStartDate), x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate continuousStartDateValueObject)
            .WithMandatoryScalarProperty(resource.BankDetails, x => UkBankDetails.CreateMandatory(nameof(BankDetails), new UkBankDetailsData { BankName = x.BankName, SortCode = x.SortCode, AccountNumber = x.AccountNumber, AccountName = x.AccountName, BuildingSocietyRollNumber = x.BuildingSocietyRollNumber }), out UkBankDetails bankDetailsValueObject)
            .Build()
            .BiBind(() => 
                Contracts.MergeChanges(resource.Contracts, envelope.Metadata, nameof(Contracts), (r, id) => EmploymentContract.Create(nameof(Contracts), r, id), (am, r) => am.Update(r, () => MarkAsUpdated(updatedActionValueObject)))
                    .BiBind(() =>
                    {
                        UpdateMandatoryValue(x => x.ContinuousStartDate, continuousStartDateValueObject, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.BankDetails, bankDetailsValueObject, updatedActionValueObject);

                        return Maybe<Fault>.None;
                    }));
    }

    internal static Result<Employment> Create(Guid tenantId, ResourceEnvelope<EmploymentResource> envelope, UserActionDto createdActionDto, Version? applicableVersion = null)
    {
        EmploymentResource resource = envelope.Data;
        
        for (int index = 0; index < resource.Contracts.Count; index++)
        {
            envelope.Metadata.GetOrAddEmbeddedMetadata(resource.Contracts[index], $"{nameof(Contracts)}[{index}]");
        }

        return Validator.CreateBuilder<Employment>()
            .WithMandatoryScalarProperty(createdActionDto, x => UserAction.CreateMandatory(nameof(CreatedAction), x), out UserAction createdActionValueObject)
            .WithMandatoryScalarProperty(resource.EmploymentReference, x => UniqueReference.CreateMandatory(nameof(EmploymentReference), x), out UniqueReference employmentReferenceValueObject)
            .WithMandatoryScalarProperty(resource.ContinuousStartDate, x => EffectiveDate.CreateMandatory(nameof(ContinuousStartDate), x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate continuousStartDateValueObject)
            .WithCollectionProperty(resource.Contracts, x => EmploymentContract.Create(nameof(Contracts), x, envelope.Metadata.GetOrAddEmbeddedMetadata(x).Id), out List<EmploymentContract> contractValueObjects)
            .WithMandatoryScalarProperty(resource.BankDetails, x => UkBankDetails.CreateMandatory(nameof(BankDetails), new UkBankDetailsData { BankName = x.BankName, SortCode = x.SortCode, AccountNumber = x.AccountNumber, AccountName = x.AccountName, BuildingSocietyRollNumber = x.BuildingSocietyRollNumber }), out UkBankDetails bankDetailsValueObject)
            .Build()
            .ToResult(() =>
                new Employment(
                    (EntityId<Employment>)envelope.Metadata.Id,
                    tenantId,
                    createdActionValueObject,
                    (EntityId<Employee>)resource.EmployeeId,
                    employmentReferenceValueObject,
                    continuousStartDateValueObject,
                    contractValueObjects,
                    bankDetailsValueObject));
    }
}
