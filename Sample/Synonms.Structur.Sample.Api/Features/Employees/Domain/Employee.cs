using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees.Domain;

[StructurResource(typeof(EmployeeResource), "employees", allowAnonymous: true, pageLimit: 5)]
public class Employee : AggregateRoot<Employee>
{
    public const int ForenameMaxLength = 100;
    public const int SurnameMaxLength = 100;
    
    private Employee()
    {
    }
    
    private Employee(
        EntityId<Employee> id,
        Guid tenantId,
        UserAction createdAction,
        UniqueReference employeeReference,
        NationalInsuranceNumber nationalInsuranceNumber,
        Moniker forename,
        Moniker surname,
        bool workPermitRequired,
        Address homeAddress,
        List<EmailContact> emailContacts,
        List<TelephoneContact> telephoneContacts,
        EmployeeEqualOpportunities equalOpportunities
        ) 
        : base(id, tenantId, createdAction)
    {
        EmployeeReference = employeeReference;
        NationalInsuranceNumber =  nationalInsuranceNumber;
        Forename = forename;
        Surname = surname;
        WorkPermitRequired = workPermitRequired;
        HomeAddress = homeAddress;
        EmailContacts = emailContacts;
        TelephoneContacts = telephoneContacts;
        EqualOpportunities = equalOpportunities;
    }

    public UniqueReference EmployeeReference { get; private set; } = null!;
    
    public NationalInsuranceNumber NationalInsuranceNumber { get; private set; } = null!;
    
    public Title? Title { get; private set; }
    
    public Moniker Forename { get; private set; } = null!;

    public Moniker? MiddleNames { get; private set; }

    public Moniker Surname { get; private set; } = null!;

    public Moniker? KnownAs { get; private set; }
    
    public bool WorkPermitRequired { get; private set; }

    public EffectiveDate? WorkPermitValidUntil { get; private set; }

    public Notes? Notes { get; private set; }

    public Address HomeAddress { get; private set; } = null!;
    
    public List<EmailContact> EmailContacts { get; private set; } = [];
    
    public List<TelephoneContact> TelephoneContacts { get; private set; } = [];
    
    public EmployeeEqualOpportunities EqualOpportunities { get; private set; }
    
    internal Maybe<Fault> Update(EmployeeResource resource, UserActionDto updatedActionDto) =>
        Validator.CreateBuilder<Employee>()
            .WithMandatoryScalarProperty(updatedActionDto, x => UserAction.CreateMandatory(nameof(UpdatedAction), x), out UserAction updatedActionValueObject)
            .WithOptionalScalarProperty(resource.Title.ToString(), x => Title.CreateOptional(nameof(Title), x), out Title? titleValueObject)
            .WithMandatoryScalarProperty(resource.Forename, x => Moniker.CreateMandatory(nameof(Forename), x, ForenameMaxLength), out Moniker forenameValueObject)
            .WithOptionalScalarProperty(resource.MiddleNames, x => Moniker.CreateOptional(nameof(MiddleNames), x), out Moniker? middleNamesValueObject)
            .WithMandatoryScalarProperty(resource.Surname, x => Moniker.CreateMandatory(nameof(Surname), x, SurnameMaxLength), out Moniker surnameValueObject)
            .WithOptionalScalarProperty(resource.KnownAs, x => Moniker.CreateOptional(nameof(KnownAs), x), out Moniker? knownAsValueObject)
            .WithOptionalScalarProperty(resource.WorkPermitValidUntil, x => EffectiveDate.CreateOptional(nameof(WorkPermitValidUntil), x), out EffectiveDate? workPermitValidUntilValueObject)
            .WithOptionalScalarProperty(resource.Notes, x => Notes.CreateOptional(nameof(Notes), x), out Notes? notesValueObject)
            .WithMandatoryScalarProperty(resource.HomeAddress, x => Address.CreateMandatory(nameof(HomeAddress), x.Type.ToString(), x.Line1, x.Line2, x.Line3, x.Line4, x.Postcode, x.Label), out Address homeAddressValueObject)
            .WithCollectionProperty(resource.EmailContacts, x => EmailContact.CreateMandatory(nameof(EmailContact), x.Address, x.IsPrimary, x.Label), out List<EmailContact> emailAddressValueObjects)
            .WithCollectionProperty(resource.TelephoneContacts, x => TelephoneContact.CreateMandatory(nameof(TelephoneContact), x.Number, x.IsPrimary, x.Label), out List<TelephoneContact> telephoneNumberValueObjects)
            .Build()
            .BiBind(() => 
                EqualOpportunities.Update(resource.EqualOpportunities, () => MarkAsUpdated(updatedActionValueObject))
                    .BiBind(() =>  
                    {
                        UpdateOptionalValue(x => x.Title, titleValueObject, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.Forename, forenameValueObject, updatedActionValueObject);
                        UpdateOptionalValue(x => x.MiddleNames, middleNamesValueObject, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.Surname, surnameValueObject, updatedActionValueObject);
                        UpdateOptionalValue(x => x.KnownAs, knownAsValueObject, updatedActionValueObject);
                        UpdateOptionalValue(x => x.WorkPermitRequired, resource.WorkPermitRequired, updatedActionValueObject);
                        UpdateOptionalValue(x => x.WorkPermitValidUntil, workPermitValidUntilValueObject, updatedActionValueObject);
                        UpdateOptionalValue(x => x.Notes, notesValueObject, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.HomeAddress, homeAddressValueObject, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.EmailContacts, emailAddressValueObjects, updatedActionValueObject);
                        UpdateMandatoryValue(x => x.TelephoneContacts, telephoneNumberValueObjects, updatedActionValueObject);
                        
                        return Maybe<Fault>.None;
                    }));

    internal static Result<Employee> Create(Guid tenantId, EmployeeResource resource, UserActionDto createdActionDto) =>
        Validator.CreateBuilder<Employee>()
            .WithMandatoryScalarProperty(createdActionDto, x => UserAction.CreateMandatory(nameof(CreatedAction), x), out UserAction createdActionValueObject)
            .WithMandatoryScalarProperty(resource.EmployeeReference, x => UniqueReference.CreateMandatory(nameof(EmployeeReference), x), out UniqueReference employeeReferenceValueObject)
            .WithMandatoryScalarProperty(resource.NationalInsuranceNumber, x => NationalInsuranceNumber.CreateMandatory(nameof(NationalInsuranceNumber), x), out NationalInsuranceNumber nationalInsuranceNumberValueObject)
            .WithOptionalScalarProperty(resource.Title.ToString(), x => Title.CreateOptional(nameof(Title), x), out Title? titleValueObject)
            .WithMandatoryScalarProperty(resource.Forename, x => Moniker.CreateMandatory(nameof(Forename), x), out Moniker forenameValueObject)
            .WithOptionalScalarProperty(resource.MiddleNames, x => Moniker.CreateOptional(nameof(MiddleNames), x), out Moniker? middleNamesValueObject)
            .WithMandatoryScalarProperty(resource.Surname, x => Moniker.CreateMandatory(nameof(Surname), x), out Moniker surnameValueObject)
            .WithOptionalScalarProperty(resource.KnownAs, x => Moniker.CreateOptional(nameof(KnownAs), x), out Moniker? knownAsValueObject)
            .WithOptionalScalarProperty(resource.WorkPermitValidUntil, x => EffectiveDate.CreateOptional(nameof(WorkPermitValidUntil), x), out EffectiveDate? workPermitValidUntilValueObject)
            .WithOptionalScalarProperty(resource.Notes, x => Notes.CreateOptional(nameof(Notes), x), out Notes? notesValueObject)
            .WithMandatoryScalarProperty(resource.HomeAddress, x => Address.CreateMandatory(nameof(HomeAddress), x.Type.ToString(), x.Line1, x.Line2, x.Line3, x.Line4, x.Postcode, x.Label), out Address homeAddressValueObject)
            .WithCollectionProperty(resource.EmailContacts, x => EmailContact.CreateMandatory(nameof(EmailContact), x.Address, x.IsPrimary, x.Label), out List<EmailContact> emailAddressValueObjects)
            .WithCollectionProperty(resource.TelephoneContacts, x => TelephoneContact.CreateMandatory(nameof(TelephoneContact), x.Number, x.IsPrimary, x.Label), out List<TelephoneContact> telephoneNumberValueObjects)
            .WithMandatoryScalarProperty(resource.EqualOpportunities, x => EmployeeEqualOpportunities.Create(nameof(EqualOpportunities), x), out EmployeeEqualOpportunities equalOpportunities)
            .Build()
            .ToResult(() =>
                new Employee((EntityId<Employee>)resource.Id, tenantId, createdActionValueObject, employeeReferenceValueObject, nationalInsuranceNumberValueObject, forenameValueObject, surnameValueObject, resource.WorkPermitRequired, homeAddressValueObject, emailAddressValueObjects, telephoneNumberValueObjects, equalOpportunities)
                {
                    Title = titleValueObject,
                    MiddleNames = middleNamesValueObject,
                    KnownAs = knownAsValueObject,
                    WorkPermitValidUntil = workPermitValidUntilValueObject,
                    Notes = notesValueObject
                });
}