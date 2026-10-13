using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Domain.System;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;
using Synonms.Structur.Infrastructure.MongoDb.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;

namespace Synonms.Structur.Tests.Integration;

public class ComplexValueObjectIntegrationTests
{
    static ComplexValueObjectIntegrationTests()
    {
        // Mirror MongoDB host startup without changing naming/discriminator conventions.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    private static AddressData AddressData(string line = "1 High Street") => new()
    {
        Type = "Home", Line1 = line, Postcode = "SW1A 1AA"
    };

    private static TSelf Create<TSelf, TData>(TData data)
        where TSelf : class, IComplexValueObject<TSelf, TData>
        where TData : class => TSelf.CreateMandatory("Value", data)
            .Match(value => value, faults => throw new InvalidOperationException(string.Join(", ", faults)));

    private static void VerifyContract<TSelf, TData>(TData data)
        where TSelf : class, IComplexValueObject<TSelf, TData>
        where TData : class
    {
        TSelf value = Create<TSelf, TData>(data);
        TSelf restored = TSelf.Rehydrate(value.ToData());
        Assert.Equal(value, restored);
        Assert.Equal(value.GetHashCode(), restored.GetHashCode());
        Assert.False(typeof(TSelf).IsValueObject());
        Assert.True(typeof(TSelf).IsComplexValueObject());
        Assert.True(TSelf.CreateOptional("Value", null).Match(maybe => maybe.IsNone, _ => false));
        Assert.True(TSelf.CreateOptional("Value", data).Match(maybe => maybe.IsSome, _ => false));
        IEnumerable<DomainRuleFault> nullFaults = TSelf.CreateMandatory("Value", null!)
            .Match(_ => throw new InvalidOperationException("Expected domain faults."), faults => faults);
        Assert.Contains(nullFaults, fault => fault.Source.Pointer == "Value");
        VerifyLegacyShape(value);
    }

    private static void VerifyLegacyShape<TSelf>(TSelf value) where TSelf : class
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        BsonClassMap<TSelf> legacyMap = new();
        legacyMap.AutoMap();
        legacyMap.Freeze();
        BsonClassMapSerializer<TSelf> legacySerializer = new(legacyMap);
        BsonDocument legacyDocument = new();
        using (BsonDocumentWriter writer = new(legacyDocument))
        {
            legacySerializer.Serialize(BsonSerializationContext.CreateRoot(writer), new BsonSerializationArgs { NominalType = typeof(TSelf) }, value);
        }
        Assert.Equal(legacyDocument, value.ToBsonDocument());
        Assert.Equal(value, BsonSerializer.Deserialize<TSelf>(legacyDocument));
    }

    [Theory]
    [InlineData(nameof(Address), "CreateMandatory")]
    [InlineData(nameof(Period), "CreateMandatory")]
    [InlineData(nameof(EmailContact), "CreateMandatory")]
    [InlineData(nameof(TelephoneContact), "CreateMandatory")]
    [InlineData(nameof(UkBankDetails), "CreateMandatory")]
    [InlineData(nameof(Address), "Rehydrate")]
    [InlineData(nameof(Period), "Rehydrate")]
    [InlineData(nameof(EmailContact), "Rehydrate")]
    [InlineData(nameof(TelephoneContact), "Rehydrate")]
    [InlineData(nameof(UkBankDetails), "Rehydrate")]
    [InlineData(nameof(Address), "CreateOptional")]
    [InlineData(nameof(Period), "CreateOptional")]
    [InlineData(nameof(EmailContact), "CreateOptional")]
    [InlineData(nameof(TelephoneContact), "CreateOptional")]
    [InlineData(nameof(UkBankDetails), "CreateOptional")]
    public void BuiltInNullInputContracts(string valueObjectType, string operation)
    {
        switch (valueObjectType)
        {
            case nameof(Address): VerifyNullInputContract<Address, AddressData>(operation); break;
            case nameof(Period): VerifyNullInputContract<Period, PeriodData>(operation); break;
            case nameof(EmailContact): VerifyNullInputContract<EmailContact, EmailContactData>(operation); break;
            case nameof(TelephoneContact): VerifyNullInputContract<TelephoneContact, TelephoneContactData>(operation); break;
            case nameof(UkBankDetails): VerifyNullInputContract<UkBankDetails, UkBankDetailsData>(operation); break;
            default: throw new ArgumentOutOfRangeException(nameof(valueObjectType));
        }
    }

    private static void VerifyNullInputContract<TSelf, TData>(string operation)
        where TSelf : class, IComplexValueObject<TSelf, TData>
        where TData : class
    {
        const string propertyName = "Parent.Value";
        switch (operation)
        {
            case "CreateMandatory":
                IEnumerable<DomainRuleFault> faults = TSelf.CreateMandatory(propertyName, null!)
                    .Match(_ => throw new InvalidOperationException("Expected domain faults."), errors => errors);
                DomainRuleFault fault = Assert.Single(faults);
                Assert.Equal(propertyName, fault.Source.Pointer);
                Assert.Equal(DomainValidationFaults.RequiredFieldTemplate, fault.Detail);
                break;
            case "Rehydrate":
                ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => TSelf.Rehydrate(null!));
                Assert.Equal("data", exception.ParamName);
                break;
            case "CreateOptional":
                Assert.True(TSelf.CreateOptional(propertyName, null).Match(maybe => maybe.IsNone, _ => false));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }

    [Fact]
    public void BuiltInsUseGenericFactoriesProjectionEqualityAndLegacyBsonShapes()
    {
        VerifyContract<Address, AddressData>(AddressData());
        VerifyContract<Period, PeriodData>(new() { Units = 3, Interval = "Day" });
        VerifyContract<EmailContact, EmailContactData>(new() { Type = "Personal", Address = "a@example.com", IsPrimary = false });
        VerifyContract<TelephoneContact, TelephoneContactData>(new() { Type = "HomeLandline", Number = "01234567890", IsPrimary = true });
        VerifyContract<UkBankDetails, UkBankDetailsData>(new() { BankName = "Bank", SortCode = "123456", AccountNumber = "12345678", AccountName = "Account" });
        Assert.Equal(Create<Address, AddressData>(AddressData()),
            Address.CreateMandatory("Address", new AddressData { Type = "Home", Line1 = "1 High Street", Line2 = null, Line3 = null, Line4 = null, Postcode = "SW1A 1AA", Label = null }).Match(value => value, _ => null!));
        Assert.Equal(Create<Period, PeriodData>(new() { Units = 3, Interval = "Day" }),
            Period.CreateMandatory("Period", new PeriodData { Units = 3, Interval = "Day" }).Match(value => value, _ => null!));
        Assert.Equal(Create<EmailContact, EmailContactData>(new() { Type = "Personal", Address = "a@example.com", IsPrimary = false }),
            EmailContact.CreateMandatory("Email", new EmailContactData { Type = "Personal", Address = "a@example.com", IsPrimary = false }).Match(value => value, _ => null!));
        Assert.Equal(Create<TelephoneContact, TelephoneContactData>(new() { Type = "HomeLandline", Number = "01234567890", IsPrimary = true }),
            TelephoneContact.CreateMandatory("Telephone", new TelephoneContactData { Type = "HomeLandline", Number = "01234567890", IsPrimary = true }).Match(value => value, _ => null!));
        Assert.Equal(Create<UkBankDetails, UkBankDetailsData>(new() { BankName = "Bank", SortCode = "123456", AccountNumber = "12345678", AccountName = "Account" }),
            UkBankDetails.CreateMandatory("Bank", new UkBankDetailsData { BankName = "Bank", SortCode = "123456", AccountNumber = "12345678", AccountName = "Account" }).Match(value => value, _ => null!));
    }

    [Fact]
    public void LegacyObjectShape()
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        Address address = Address.CreateMandatory("Address", new AddressData { Type = "Home", Line1 = "1 High Street", Line2 = null, Line3 = null, Line4 = null, Postcode = "SW1A 1AA", Label = null })
            .Match(value => value, faults => throw new InvalidOperationException(string.Join(", ", faults)));
        BsonDocument document = address.ToBsonDocument();
        Assert.Equal(new BsonDocument
        {
            { "Type", "Home" }, { "Line1", "1 High Street" }, { "Line2", BsonNull.Value },
            { "Line3", BsonNull.Value }, { "Line4", BsonNull.Value }, { "Postcode", "SW1A 1AA" },
            { "Label", BsonNull.Value }
        }, document);
        Address restored = BsonSerializer.Deserialize<Address>(document);
        Assert.Equal(address.Line1.Value, restored.Line1.Value);
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Address>(new BsonDocument()));
        document["Line1"] = BsonNull.Value;
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Address>(document));
        document["Line1"] = new BsonDocument("sensitive", "do not disclose");
        BsonSerializationException malformed = Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Address>(document));
        Assert.Contains("Address", malformed.Message);
        Assert.DoesNotContain("sensitive", malformed.ToString());
    }

    [Fact]
    public void PartialOptionalInputRemainsNoneAndMandatoryMissingMembersHaveNestedFaultPaths()
    {
        Assert.True(Address.CreateOptional("Address", new AddressData { Type = "Home", Line1 = "1 High Street" }).Match(maybe => maybe.IsNone, _ => false));
        Assert.True(Period.CreateOptional("Period", new PeriodData { Interval = "invalid" }).Match(maybe => maybe.IsNone, _ => false));
        Assert.True(Period.CreateOptional("Period", new PeriodData { Units = 0, Interval = "Day" }).Match(maybe => maybe.IsSome, _ => false));
        Assert.True(EmailContact.CreateOptional("Email", new EmailContactData { Type = "Home", Address = "invalid" }).Match(maybe => maybe.IsNone, _ => false));
        Assert.True(TelephoneContact.CreateOptional("Telephone", new TelephoneContactData { Type = "Home", IsPrimary = false }).Match(maybe => maybe.IsNone, _ => false));
        Assert.True(UkBankDetails.CreateOptional("Bank", new UkBankDetailsData { BankName = "Bank" }).Match(maybe => maybe.IsNone, _ => false));
        Assert.True(Address.CreateMandatory("Address", new AddressData()).Match(_ => false, faults => faults.Any(f => f.Source.Pointer == "Address.Line1")));
        Assert.True(Period.CreateMandatory("Period", new PeriodData { Interval = "Day" }).Match(_ => false, faults => faults.Any(f => f.Source.Pointer == "Period.Units")));
        Assert.True(EmailContact.CreateMandatory("Email", new EmailContactData { Type = "Home", Address = "invalid" }).Match(_ => false,
            faults => faults.Any(f => f.Source.Pointer == "Email.Address") && faults.Any(f => f.Source.Pointer == "Email.IsPrimary")));
        Assert.True(AddressBook.CreateMandatory("Book", new() { Addresses = [new()] }).Match(_ => false,
            faults => faults.Any(f => f.Source.Pointer == "Book.Addresses[0].Line1")));
    }

    [Fact]
    public void CollectionFixtureOwnsInputAndProjectionAndUsesOrderedExactTypeEquality()
    {
        List<AddressData> input = [AddressData(), AddressData("2 High Street")];
        AddressBook first = Create<AddressBook, AddressBookData>(new() { Addresses = input });
        AddressBook second = Create<AddressBook, AddressBookData>(first.ToData());
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, AddressBook.Rehydrate(new() { Addresses = input.AsEnumerable().Reverse().ToList() }));
        Assert.NotEqual(first, new DerivedAddressBook(first.Addresses));
        Assert.NotEqual(new DerivedAddressBook(first.Addresses), first);
        input.Clear();
        first.ToData().Addresses!.Clear();
        Assert.Equal(2, first.Addresses.Count);
        Assert.Throws<NotSupportedException>(() => ((IList<Address>)first.Addresses).Clear());
        AddressBookResource mapped = Assert.IsType<AddressBookResource>(
            MappingHelper.ConvertValue(typeof(AddressBook), typeof(AddressBookResource), first));
        Assert.Equal("preserved", mapped.UnmatchedMetadata);
        Assert.Equal(2, mapped.Addresses!.Count);
        mapped.Addresses.Clear();
        Assert.Equal(2, first.Addresses.Count);
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        Assert.Equal(first, BsonSerializer.Deserialize<AddressBook>(first.ToBsonDocument()));
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<AddressBook>(
            new BsonDocument("Addresses", new BsonArray { BsonNull.Value })));
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<AddressBook>(
            new BsonDocument("Addresses", BsonNull.Value)));
        AddressBook empty = Create<AddressBook, AddressBookData>(new() { Addresses = [] });
        Assert.Empty(Assert.IsType<AddressBookResource>(MappingHelper.ConvertValue(typeof(AddressBook), typeof(AddressBookResource), empty)).Addresses!);
        Assert.Equal(empty, BsonSerializer.Deserialize<AddressBook>(empty.ToBsonDocument()));
        AddressResource?[] array = Assert.IsType<AddressResource[]>(
            MappingHelper.ConvertValue(typeof(Address[]), typeof(AddressResource[]), first.Addresses.ToArray()));
        Assert.Equal(2, array.Length);
        Assert.Equal("2 High Street", array[1]!.Line1);
    }

    [Fact]
    public void DefaultMapperAndResourceJsonRetainObjectShapeEnumsNullCollectionsAndMetadata()
    {
        Address address = Create<Address, AddressData>(AddressData());
        ComplexAggregate aggregate = new()
        {
            Address = address,
            Book = Create<AddressBook, AddressBookData>(new() { Addresses = [AddressData()] }),
            Addresses = [address, null],
            Period = Create<Period, PeriodData>(new() { Units = 2, Interval = "Day" }),
            Email = Create<EmailContact, EmailContactData>(new() { Type = "Personal", Address = "a@example.com", IsPrimary = false }),
            Telephone = Create<TelephoneContact, TelephoneContactData>(new() { Type = "HomeLandline", Number = "01234567890", IsPrimary = true }),
            Bank = Create<UkBankDetails, UkBankDetailsData>(new() { BankName = "Bank", SortCode = "123456", AccountNumber = "12345678", AccountName = "Account" })
        };
        DefaultResourceMapper<ComplexAggregate, ComplexResource> mapper = new(
            NullLogger<DefaultResourceMapper<ComplexAggregate, ComplexResource>>.Instance,
            new EmptyResourceMapperFactory(), new EmptyChildResourceMapperFactory(), new FixtureRouteGenerator());
        ResourceEnvelope<ComplexResource> envelope = mapper.MapEnvelope(aggregate);
        ComplexResource resource = envelope.Data;
        Assert.Equal(aggregate.Id.Value, envelope.Metadata.Id);
        Assert.Equal("/complex/123", envelope.Metadata.SelfLink.Uri.ToString());
        Assert.NotEmpty(envelope.Metadata.Links);
        Assert.Equal(AddressTypeEnumeration.Home, resource.Address!.Type);
        Assert.Null(resource.Address.Line2);
        Assert.Null(resource.Addresses![1]);
        Assert.Equal(2, resource.Period!.Units);
        Assert.False(resource.Email!.IsPrimary);
        Assert.True(resource.Telephone!.IsPrimary);
        Assert.Equal("123456", resource.Bank!.SortCode);
        JsonSerializerOptions options = new JsonSerializerOptions().ConfigureForStructurFramework();
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new DefaultCustomJsonConverterFactory(new Version(1, 0)));
        options.Converters.Add(new DefaultLinkJsonConverter());
        string json = JsonSerializer.Serialize(new ResourceDocument<ComplexResource>(envelope), options);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement.GetProperty(DefaultPropertyNames.Value);
        Assert.Equal("Home", root.GetProperty("address").GetProperty("type").GetString());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("address").GetProperty("line2").ValueKind);
        Assert.Equal(JsonValueKind.Null, root.GetProperty("addresses")[1].ValueKind);
        Assert.Equal("1 High Street", root.GetProperty("book").GetProperty("addresses")[0].GetProperty("line1").GetString());
        ComplexResource input = JsonSerializer.Deserialize<ComplexResource>(
            """{"email":{"type":"Personal","address":"not-an-email","isPrimary":false}}""", options)!;
        Assert.Equal("not-an-email", input.Email!.Address);
        Assert.True(EmailContact.CreateMandatory("Email", new EmailContactData { Type = input.Email.Type.ToString(), Address = input.Email.Address, IsPrimary = input.Email.IsPrimary })
            .Match(_ => false, faults => faults.Any(fault => fault.Source.Pointer == "Email.Address")));
        ComplexResource empty = mapper.Map(new ComplexAggregate());
        Assert.Null(empty.Address);
        Assert.Null(empty.Book);
        Assert.Null(empty.Addresses);
    }

    [Fact]
    public void BsonRecordRoundTripMissingOptionalsAndNestedScalarFallbackRemainSupported()
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        ComplexRecord record = new()
        {
            Address = Create<Address, AddressData>(AddressData()),
            Book = Create<AddressBook, AddressBookData>(new() { Addresses = [AddressData()] }),
            Addresses = [null, Create<Address, AddressData>(AddressData("2 High Street"))],
            Period = Period.Rehydrate(new() { Units = -1, Interval = "Day" }),
            Email = EmailContact.Rehydrate(new() { Type = "Personal", Address = "invalid", IsPrimary = false })
        };
        ComplexRecord copied = MongoDbRecordMapper.Map<ComplexRecord>(record);
        Assert.Equal(record.Address, copied.Address);
        Assert.Equal(record.Book, copied.Book);
        Assert.Equal(record.Addresses, copied.Addresses);
        Assert.Equal(0, copied.Period!.Units.Value);
        Assert.Equal(string.Empty, copied.Email!.Address.Value);
        Assert.False(copied.Email.IsPrimary);
        Assert.Equal(string.Empty, copied.Email.ToData().Address);
        Assert.True(EmailContact.CreateMandatory("Email", copied.Email.ToData()).Match(_ => false, faults => faults.Any()));
        Assert.Null(BsonSerializer.Deserialize<ComplexRecord>(new BsonDocument()).Address);
        Assert.Null(BsonSerializer.Deserialize<ComplexRecord>(new BsonDocument("Address", BsonNull.Value)).Address);
        BsonDocument periodDocument = new() { { "Units", -10 }, { "Interval", "Day" } };
        Assert.Equal(0, BsonSerializer.Deserialize<Period>(periodDocument).Units.Value);
        periodDocument.Remove("Units");
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Period>(periodDocument));
        periodDocument["Units"] = BsonNull.Value;
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Period>(periodDocument));
        periodDocument["Units"] = new BsonArray();
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<Period>(periodDocument));
        BsonDocument addressDocument = record.Address!.ToBsonDocument();
        addressDocument.Remove("Line2");
        Assert.Null(BsonSerializer.Deserialize<Address>(addressDocument).Line2);
        BsonDocument emailDocument = record.Email!.ToBsonDocument();
        emailDocument.Remove("IsPrimary");
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<EmailContact>(emailDocument));
        emailDocument["IsPrimary"] = BsonNull.Value;
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<EmailContact>(emailDocument));
        emailDocument["IsPrimary"] = "false";
        Assert.False(BsonSerializer.Deserialize<EmailContact>(emailDocument).IsPrimary);
        emailDocument["IsPrimary"] = new BsonDocument("unexpected", true);
        Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<EmailContact>(emailDocument));
    }

    [Fact]
    public void AggregateRecordRoundTripPreservesComplexGraphsIdentityTenantTagAndAudit()
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        UserAction action = UserAction.CreateMandatory("CreatedAction", new UserActionDto
        {
            ActionAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
            ActionById = Guid.NewGuid(),
            ActionByName = "Test User"
        }).Match(value => value, _ => throw new InvalidOperationException());
        ComplexAggregate aggregate = new(EntityId<ComplexAggregate>.New(), Guid.NewGuid(), action)
        {
            Address = Create<Address, AddressData>(AddressData()),
            Book = Create<AddressBook, AddressBookData>(new() { Addresses = [AddressData()] }),
            Addresses = [null, Create<Address, AddressData>(AddressData("2 High Street"))]
        };
        BsonDocument document = MongoDbRecordMapper.MapToRecordDocument(aggregate, typeof(ComplexRecord));
        ComplexAggregate restored = MongoDbRecordMapper.MapToAggregateRoot<ComplexAggregate>(document, typeof(ComplexRecord));
        Assert.Equal(aggregate.Address, restored.Address);
        Assert.Equal(aggregate.Book, restored.Book);
        Assert.Equal(aggregate.Addresses, restored.Addresses);
        Assert.Equal(aggregate.Id.Value, restored.Id.Value);
        Assert.Equal(aggregate.TenantId, restored.TenantId);
        Assert.Equal(aggregate.EntityTag.Value, restored.EntityTag.Value);
        Assert.Equal(action.ActionAt, restored.CreatedAction.ActionAt);
        Assert.Equal(action.ActionById, restored.CreatedAction.ActionById);
        Assert.Equal(action.ActionByName, restored.CreatedAction.ActionByName);
        Assert.Null(restored.UpdatedAction);
        Assert.Null(restored.DeletedAction);
        Assert.Equal(document, MongoDbRecordMapper.MapToRecordDocument(restored, typeof(ComplexRecord)));
    }

    [Fact]
    public void BsonDocumentMetadataSupportsNestedScalarAndCollectionQueries()
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        IBsonDocumentSerializer serializer = Assert.IsAssignableFrom<IBsonDocumentSerializer>(BsonSerializer.LookupSerializer<Address>());
        Assert.True(serializer.TryGetMemberSerializationInfo("Type", out BsonSerializationInfo typeInfo));
        Assert.Equal("Type", typeInfo.ElementName);
        Assert.Equal(typeof(AddressType), typeInfo.NominalType);
        Assert.False(serializer.TryGetMemberSerializationInfo("UnknownMember", out _));
        RenderArgs<ComplexRecord> args = new(BsonSerializer.LookupSerializer<ComplexRecord>(), BsonSerializer.SerializerRegistry);
        Assert.Equal(new BsonDocument("Address.Type", "Home"),
            Builders<ComplexRecord>.Filter.Eq(record => record.Address!.Type, AddressType.From(AddressTyeEnumeration.Home)).Render(args));
        Assert.Equal(new BsonDocument("Book.Addresses", new BsonDocument("$elemMatch", new BsonDocument("Line1", "1 High Street"))),
            Builders<ComplexRecord>.Filter.ElemMatch(record => record.Book!.Addresses,
                Builders<Address>.Filter.Eq(address => address.Line1, AddressLine.Rehydrate("1 High Street"))).Render(args));
    }

    [Fact]
    public void ExplicitSerializerAndMappingAdaptersRetainPrecedence()
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom(typeof(AdaptedAddressBsonSerialiser).Assembly);
        StructurBsonSerialisation.TryRegisterSerialisersFrom(typeof(AdaptedAddressBsonSerialiser).Assembly);
        Assert.IsType<AdaptedAddressBsonSerialiser>(BsonSerializer.LookupSerializer<AdaptedAddress>());
        AdaptedRecord record = new() { Address = Create<AdaptedAddress, AddressData>(AddressData()) };
        Assert.Equal("1 High Street", record.ToBsonDocument()["Address"].AsString);
        Assert.Equal("1 High Street", MongoDbRecordMapper.Map<AdaptedRecord>(record).Address!.Address.Line1.Value);
        ComplexValueObjectResourceMapping.Register<AdaptedAddress, AdaptedResource>(
            address => new() { Street = address.Address.Line1.Value });
        Assert.Equal("1 High Street", Assert.IsType<AdaptedResource>(
            MappingHelper.ConvertValue(typeof(AdaptedAddress), typeof(AdaptedResource), record.Address)).Street);
    }
}
