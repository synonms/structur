using System.Collections.ObjectModel;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Tests.Integration;

// A representative collection-bearing fixture, not an additional framework domain concept.
public sealed class AddressBookData
{
    public List<AddressData>? Addresses { get; init; }
}

public class AddressBook : ComplexValueObject<AddressBook>, IComplexValueObject<AddressBook, AddressBookData>
{
    protected AddressBook(IEnumerable<Address> addresses)
    {
        Addresses = new ReadOnlyCollection<Address>(addresses.ToArray());
    }

    public IReadOnlyList<Address> Addresses { get; }

    public AddressBookData ToData() => new() { Addresses = Addresses.Select(address => address.ToData()).ToList() };

    public static OneOf<AddressBook, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, AddressBookData data)
    {
        if (data?.Addresses is null)
        {
            return Validator.CreateBuilder<AddressBook>()
                .WithFaultIf(propertyName, () => true, DomainValidationFaults.RequiredFieldTemplate)
                .Build(() => null!);
        }
        return Validator.CreateBuilder<AddressBook>()
            .WithCollectionProperty(data.Addresses.Select((address, index) => (address, index)).ToList(),
                item => Address.CreateMandatory($"{propertyName}.Addresses[{item.index}]", item.address),
                out List<Address> addresses)
            .Build(() => new AddressBook(addresses));
    }

    public static OneOf<Maybe<AddressBook>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, AddressBookData? data) =>
        data?.Addresses is null ? Maybe<AddressBook>.None : CreateMandatory(propertyName, data).ToMaybe();

    public static AddressBook Rehydrate(AddressBookData data) =>
        new(ComplexValueObjectData.Required(data.Addresses, nameof(Addresses)).Select(Address.Rehydrate));

    protected override object?[] EqualityMembers => [Addresses];
}

public sealed class DerivedAddressBook(IEnumerable<Address> addresses) : AddressBook(addresses);

public sealed class AddressBookResource : ComplexValueObjectResource
{
    public List<AddressResource>? Addresses { get; set; }
    public string UnmatchedMetadata { get; set; } = "preserved";
}

public sealed class ComplexAggregate : AggregateRoot<ComplexAggregate>
{
    public ComplexAggregate() { }
    public ComplexAggregate(EntityId<ComplexAggregate> id, Guid tenantId, UserAction action) : base(id, tenantId, action) { }
    public Address? Address { get; set; }
    public AddressBook? Book { get; set; }
    public List<Address?>? Addresses { get; set; }
    public Period? Period { get; set; }
    public EmailContact? Email { get; set; }
    public TelephoneContact? Telephone { get; set; }
    public UkBankDetails? Bank { get; set; }
}

public sealed class ComplexResource : Resource
{
    public override string GetCollectionPath() => "/complex";

    public AddressResource? Address { get; set; }
    public AddressBookResource? Book { get; set; }
    public List<AddressResource?>? Addresses { get; set; }
    public PeriodResource? Period { get; set; }
    public EmailContactResource? Email { get; set; }
    public TelephoneContactResource? Telephone { get; set; }
    public UkBankDetailsResource? Bank { get; set; }
}

public sealed class ComplexRecord
{
    public EntityId<ComplexAggregate> Id { get; set; } = EntityId<ComplexAggregate>.Uninitialised;
    public Guid TenantId { get; set; }
    public EntityTag EntityTag { get; set; } = EntityTag.Uninitialised;
    public UserActionRecord CreatedAction { get; set; } = new();
    public UserActionRecord? UpdatedAction { get; set; }
    public UserActionRecord? DeletedAction { get; set; }

    public Address? Address { get; set; }
    public AddressBook? Book { get; set; }
    public List<Address?>? Addresses { get; set; }
    public Period? Period { get; set; }
    public EmailContact? Email { get; set; }
    public TelephoneContact? Telephone { get; set; }
    public UkBankDetails? Bank { get; set; }
}

public sealed class EmptyResourceMapperFactory : IResourceMapperFactory
{
    public IResourceMapper? Create(Type aggregateRootType, Type resourceType) => null;
}

public sealed class EmptyChildResourceMapperFactory : IChildResourceMapperFactory
{
    public IChildResourceMapper? Create(Type aggregateMemberType, Type childResourceType) => null;
}

public sealed class FixtureRouteGenerator : IRouteGenerator
{
    private static Uri Route => new("/complex/123", UriKind.Relative);
    public Uri Item<T>(EntityId<T> id, QueryParameters? queryParameters = null) where T : AggregateRoot<T> => Route;
    public Uri Item(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null) => Route;
    public Uri Collection<T>(QueryParameters? queryParameters = null) where T : AggregateRoot<T> => Route;
    public Uri Collection(Type aggregateRootType, QueryParameters? queryParameters = null) => Route;
    public Uri CreateForm<T>(QueryParameters? queryParameters = null) where T : AggregateRoot<T> => Route;
    public Uri CreateForm(Type aggregateRootType, QueryParameters? queryParameters = null) => Route;
    public Uri EditForm<T>(EntityId<T> id, QueryParameters? queryParameters = null) where T : AggregateRoot<T> => Route;
    public Uri EditForm(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null) => Route;
    public Uri Projection<T, TProjection>(EntityId<T> id, QueryParameters? queryParameters = null)
        where T : AggregateRoot<T> where TProjection : Projection<T> => Route;
    public Uri Projection(Type aggregateRootType, Type projectionType, Guid id, QueryParameters? queryParameters = null) => Route;
}
