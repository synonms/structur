using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Domain;

[StructurResource(typeof(IndividualResource), "individuals", allowAnonymous: true, pageLimit: 5)]
public class Individual : AggregateRoot<Individual>
{
    public const int ForenameMaxLength = 100;
    public const int SurnameMaxLength = 100;
    
    private Individual()
    {
    }
    
    private Individual(
        EntityId<Individual> id,
        Guid tenantId,
        UserAction createdAction,
        ExternalReference tenantReference,
        FriendlyId friendlyId,
        Moniker forename,
        Moniker surname,
        List<EmailAddress> emailAddresses,
        List<TelephoneNumber> telephoneNumbers
        ) 
        : base(id, tenantId, createdAction)
    {
        TenantReference = tenantReference;
        FriendlyId = friendlyId;
        Forename = forename;
        Surname = surname;
        EmailAddresses = emailAddresses;
        TelephoneNumbers = telephoneNumbers;
    }

    public ExternalReference TenantReference { get; private set; } = null!;
    
    public FriendlyId FriendlyId { get; private set; } = null!;

    public Salutation? Salutation { get; private set; }
    
    public Moniker Forename { get; private set; } = null!;

    public Moniker Surname { get; private set; } = null!;
    
    public List<EmailAddress> EmailAddresses { get; private set; } = [];
    
    public List<TelephoneNumber> TelephoneNumbers { get; private set; } = [];
    
    internal Maybe<Fault> Update(IndividualResource resource, UserActionDto updatedActionDto) =>
        Entity.CreateBuilder<Individual>()
            .WithMandatoryValueObject(updatedActionDto, x => UserAction.CreateMandatory(nameof(UpdatedAction), x), out UserAction updatedActionValueObject)
            .WithOptionalValueObject(resource.Salutation, x => Salutation.CreateOptional(nameof(Salutation), x), out Salutation? salutationValueObject)
            .WithMandatoryValueObject(resource.Forename, x => Moniker.CreateMandatory(nameof(Forename), x, ForenameMaxLength), out Moniker forenameValueObject)
            .WithMandatoryValueObject(resource.Surname, x => Moniker.CreateMandatory(nameof(Surname), x, SurnameMaxLength), out Moniker surnameValueObject)
            .WithValueObjectCollection(resource.EmailAddresses, x => EmailAddress.CreateMandatory(nameof(EmailAddress), x), out List<EmailAddress> emailAddressValueObjects)
            .WithValueObjectCollection(resource.TelephoneNumbers, x => TelephoneNumber.CreateMandatory(nameof(TelephoneNumber), x), out List<TelephoneNumber> telephoneNumberValueObjects)
            .Build()
            .BiBind(() => 
            {
                UpdateOptionalValue(x => x.Salutation, salutationValueObject, updatedActionValueObject);
                UpdateMandatoryValue(x => x.Forename, forenameValueObject, updatedActionValueObject);
                UpdateMandatoryValue(x => x.Surname, surnameValueObject, updatedActionValueObject);
                UpdateMandatoryValue(x => x.EmailAddresses, emailAddressValueObjects, updatedActionValueObject);
                UpdateMandatoryValue(x => x.TelephoneNumbers, telephoneNumberValueObjects, updatedActionValueObject);

                return Maybe<Fault>.None;
            });

    internal static Result<Individual> Create(Guid tenantId, IndividualResource resource, UserActionDto createdActionDto) =>
        Entity.CreateBuilder<Individual>()
            .WithMandatoryValueObject(createdActionDto, x => UserAction.CreateMandatory(nameof(CreatedAction), x), out UserAction createdActionValueObject)
            .WithMandatoryValueObject(resource.TenantReference, x => ExternalReference.CreateMandatory(nameof(TenantReference), x), out ExternalReference tenantReferenceValueObject)
            .WithOptionalValueObject(resource.Salutation, x => Salutation.CreateOptional(nameof(Salutation), x), out Salutation? salutationValueObject)
            .WithMandatoryValueObject(resource.Forename, x => Moniker.CreateMandatory(nameof(Forename), x), out Moniker forenameValueObject)
            .WithMandatoryValueObject(resource.Surname, x => Moniker.CreateMandatory(nameof(Surname), x), out Moniker surnameValueObject)
            .WithValueObjectCollection(resource.EmailAddresses, x => EmailAddress.CreateMandatory(nameof(EmailAddress), x), out List<EmailAddress> emailAddressValueObjects)
            .WithValueObjectCollection(resource.TelephoneNumbers, x => TelephoneNumber.CreateMandatory(nameof(TelephoneNumber), x), out List<TelephoneNumber> telephoneNumberValueObjects)
            .Build()
            .ToResult(() =>
                new Individual((EntityId<Individual>)resource.Id, tenantId, createdActionValueObject, tenantReferenceValueObject, FriendlyId.New(), forenameValueObject, surnameValueObject, emailAddressValueObjects, telephoneNumberValueObjects)
                {
                    Salutation = salutationValueObject
                });
}