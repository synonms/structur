using System.Collections;
using Microsoft.Extensions.Logging.Abstractions;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;

namespace Synonms.Structur.Tests.Integration;

public class ComplexMappingRefinementIntegrationTests
{
    private static AddressData Data(string line) => new() { Type = "Home", Line1 = line, Postcode = "SW1A 1AA" };

    [Fact]
    public void NestedNonGenericListSubclassMapsElementsInOrderIncludingNulls()
    {
        DerivedListData data = new() { Addresses = [Data("first"), null, Data("last")] };
        SequenceResource resource = Assert.IsType<SequenceResource>(
            ComplexValueObjectResourceMapping.Map(typeof(DerivedListData), typeof(SequenceResource), data));
        Assert.Equal(new[] { "first", null, "last" }, resource.Addresses!.Select(address => address?.Line1));
        Assert.Equal("not a sequence", resource.Label);
        Assert.NotSame(data.Addresses, resource.Addresses);
        data.Addresses!.Clear();
        Assert.Equal(3, resource.Addresses!.Count);
        data.Addresses = null;
        Assert.Null(Assert.IsType<SequenceResource>(
            ComplexValueObjectResourceMapping.Map(typeof(DerivedListData), typeof(SequenceResource), data)).Addresses);
    }

    [Fact]
    public void ActualEnumerableInterfaceNotFirstGenericArgumentDeterminesElementType()
    {
        CustomSequenceData data = new() { Addresses = new MarkerSequence<int, AddressData?>([Data("first"), null, Data("last")]) };
        SequenceResource resource = Assert.IsType<SequenceResource>(
            ComplexValueObjectResourceMapping.Map(typeof(CustomSequenceData), typeof(SequenceResource), data));
        Assert.Equal(new[] { "first", null, "last" }, resource.Addresses!.Select(address => address?.Line1));
        foreach (Type destination in new[] { typeof(IEnumerable<AddressResource>), typeof(IReadOnlyList<AddressResource>), typeof(AddressResource[]) })
        {
            IEnumerable<AddressResource?> mapped = Assert.IsAssignableFrom<IEnumerable<AddressResource?>>(
                ComplexValueObjectResourceMapping.Map(data.Addresses!.GetType(), destination, data.Addresses));
            Assert.Equal(new[] { "first", null, "last" }, mapped.Select(address => address?.Line1));
        }
        Assert.Empty(Assert.IsType<SequenceResource>(ComplexValueObjectResourceMapping.Map(
            typeof(CustomSequenceData), typeof(SequenceResource),
            new CustomSequenceData { Addresses = new MarkerSequence<int, AddressData?>([]) })).Addresses!);
    }

    [Fact]
    public void MappingHelperRoutesInheritedAndCustomComplexCollections()
    {
        AddressCollection addresses = [Address.Rehydrate(Data("first")), null, Address.Rehydrate(Data("last"))];
        MarkerSequence<int, Address?> sequence = new(addresses);
        foreach (object source in new object[] { addresses, sequence })
        {
            Type sourceType = source.GetType();
            List<AddressResource?> converted = Assert.IsType<List<AddressResource?>>(MappingHelper.ConvertValue(
                sourceType, typeof(List<AddressResource?>), source));
            List<AddressResource?> mapped = Assert.IsType<List<AddressResource?>>(MappingHelper.MapVanillaCollection(
                sourceType, typeof(List<AddressResource?>), source));
            Assert.Equal(new[] { "first", null, "last" }, converted.Select(address => address?.Line1));
            Assert.Equal(new[] { "first", null, "last" }, mapped.Select(address => address?.Line1));
        }
    }

    [Fact]
    public void UnsupportedEnumerableShapesFailDiagnosticallyInsteadOfMappingDtoProperties()
    {
        foreach (Type unsupported in new[] { typeof(ArrayList), typeof(AmbiguousSequence) })
        {
            object source = Activator.CreateInstance(unsupported)!;
            NotSupportedException sourceError = Assert.Throws<NotSupportedException>(() =>
                ComplexValueObjectResourceMapping.Map(unsupported, typeof(List<AddressResource>), source));
            Assert.Contains("IEnumerable<T>", sourceError.Message);
            Assert.Contains("adapter", sourceError.Message);
            Assert.Throws<NotSupportedException>(() =>
                ComplexValueObjectResourceMapping.Map(typeof(List<AddressData>), unsupported, new List<AddressData>()));
        }
        Assert.Throws<NotSupportedException>(() => ComplexValueObjectResourceMapping.Map(
            typeof(List<AddressData>), typeof(AddressListResource), new List<AddressData>()));
        Assert.Throws<NotSupportedException>(() => ComplexValueObjectResourceMapping.Map(
            typeof(List<AddressData>), typeof(AddressResource), new List<AddressData>()));
        Assert.Throws<NotSupportedException>(() => ComplexValueObjectResourceMapping.Map(
            typeof(AddressData[,]), typeof(AddressResource[]), new AddressData[1, 1]));
    }

    [Fact]
    public void NestedDtoAdapterRegisteredAfterPlanCreationOverridesCachedBindings()
    {
        AddressBook book = AddressBook.Rehydrate(new() { Addresses = [Data("original")] });
        AdapterBookResource Map() => Assert.IsType<AdapterBookResource>(
            ComplexValueObjectResourceMapping.Map(typeof(AddressBook), typeof(AdapterBookResource), book));
        Assert.Equal("original", Map().Addresses![0].Line1);
        ComplexValueObjectResourceMapping.Register<Address, AdapterAddressResource>(_ => new() { Line1 = "domain adapter" });
        Assert.Equal("domain adapter", Assert.IsType<AdapterAddressResource>(
            ComplexValueObjectResourceMapping.Map(typeof(Address), typeof(AdapterAddressResource), book.Addresses[0])).Line1);
        // AddressBook.ToData() contains AddressData, not Address: the domain adapter cannot apply.
        Assert.Equal("original", Map().Addresses![0].Line1);
        ComplexValueObjectResourceMapping.Register<AddressData, AdapterAddressResource>(_ => new() { Line1 = "DTO adapter" });
        Assert.Equal("DTO adapter", Map().Addresses![0].Line1);
        Assert.Equal("original", book.Addresses[0].Line1.Value);
    }

    [Fact]
    public async Task ConcurrentFirstUseBuildsIndependentCachedMappings()
    {
        TaskCompletionSource start = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Task<ConcurrentBookResource>[] tasks = Enumerable.Range(0, 32).Select(async index =>
        {
            await start.Task;
            ConcurrentBook book = ConcurrentBook.Rehydrate(new() { Addresses = [Data(index.ToString())] });
            return Assert.IsType<ConcurrentBookResource>(
                ComplexValueObjectResourceMapping.Map(typeof(ConcurrentBook), typeof(ConcurrentBookResource), book));
        }).ToArray();
        start.SetResult();
        ConcurrentBookResource[] resources = await Task.WhenAll(tasks);
        Assert.Equal(Enumerable.Range(0, 32).Select(index => index.ToString()),
            resources.Select(resource => resource.Addresses![0].Line1));
        resources[0].Addresses!.Clear();
        Assert.Single(resources[1].Addresses!);
    }

    [Fact]
    public void DefaultChildMapperPreservesComplexPropertiesCollectionsAndIdentity()
    {
        ComplexMember member = new(EntityId<ComplexMember>.New())
        {
            Address = Address.Rehydrate(Data("child")),
            Addresses = [null, Address.Rehydrate(Data("last"))],
            Book = AddressBook.Rehydrate(new() { Addresses = [Data("nested")] })
        };
        DefaultChildResourceMapper<ComplexMember, ComplexChildResource> mapper = new(
            NullLogger<DefaultChildResourceMapper<ComplexMember, ComplexChildResource>>.Instance,
            new EmptyResourceMapperFactory(), new EmptyChildResourceMapperFactory(), new FixtureRouteGenerator());
        ResourceEnvelope<ComplexChildResource> envelope = mapper.MapEnvelope(member)!;
        ComplexChildResource resource = envelope.Data;
        Assert.Equal(member.Id.Value, envelope.Metadata.Id);
        Assert.Equal("child", resource.Address!.Line1);
        Assert.Equal(new[] { null, "last" }, resource.Addresses!.Select(address => address?.Line1));
        Assert.Equal("nested", resource.Book!.Addresses![0].Line1);
        ResourceEnvelope<ComplexChildResource> emptyEnvelope = mapper.MapEnvelope(new ComplexMember(EntityId<ComplexMember>.New()))!;
        ComplexChildResource empty = emptyEnvelope.Data;
        Assert.Null(empty.Address);
        Assert.Null(empty.Addresses);
        Assert.Null(empty.Book);
    }

    [Fact]
    public void BuiltInRequiredMembersAndCompleteInvalidOptionalsKeepExistingPolicies()
    {
        VerifyPolicies<Address, AddressData>(Data("valid"),
            new() { Type = "invalid", Line1 = "valid", Postcode = "SW1A 1AA" }, ["Type", "Line1", "Postcode"]);
        VerifyPolicies<Period, PeriodData>(new() { Units = 0, Interval = "Day" },
            new() { Units = 1, Interval = "invalid" }, ["Units", "Interval"]);
        VerifyPolicies<EmailContact, EmailContactData>(new() { Type = "Personal", Address = "a@example.com", IsPrimary = false },
            new() { Type = "Personal", Address = "invalid", IsPrimary = false }, ["Type", "Address", "IsPrimary"]);
        VerifyPolicies<TelephoneContact, TelephoneContactData>(new() { Type = "HomeLandline", Number = "01234567890", IsPrimary = false },
            new() { Type = "invalid", Number = "01234567890", IsPrimary = false }, ["Type", "Number", "IsPrimary"]);
        VerifyPolicies<UkBankDetails, UkBankDetailsData>(new() { BankName = "Bank", SortCode = "123456", AccountNumber = "12345678", AccountName = "Account" },
            new() { BankName = "Bank", SortCode = "invalid", AccountNumber = "12345678", AccountName = "Account" },
            ["BankName", "SortCode", "AccountNumber", "AccountName"]);
    }

    private static void VerifyPolicies<TSelf, TData>(TData valid, TData invalid, string[] requiredMembers)
        where TSelf : class, IComplexValueObject<TSelf, TData>
        where TData : class, new()
    {
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        Assert.True(TSelf.CreateMandatory("Value", valid).Match(_ => true, _ => false));
        Assert.True(TSelf.CreateOptional("Value", valid).Match(maybe => maybe.IsSome, _ => false));
        Assert.True(TSelf.CreateOptional("Value", invalid).Match(_ => false, faults => faults.Any()));
        BsonDocument validDocument = TSelf.Rehydrate(valid).ToBsonDocument();
        Assert.Equal(validDocument, BsonSerializer.Deserialize<TSelf>(validDocument).ToBsonDocument());
        foreach (string member in requiredMembers)
        {
            TData missing = new();
            foreach (System.Reflection.PropertyInfo property in typeof(TData).GetProperties())
            {
                property.SetValue(missing, property.Name == member ? null : property.GetValue(valid));
            }
            Assert.True(TSelf.CreateMandatory("Value", missing).Match(_ => false,
                faults => faults.Any(fault => fault.Source.Pointer == $"Value.{member}")));
            Assert.True(TSelf.CreateOptional("Value", missing).Match(maybe => maybe.IsNone, _ => false));
            Assert.Throws<ArgumentException>(() => TSelf.Rehydrate(missing));
            BsonDocument document = validDocument.DeepClone().AsBsonDocument;
            document.Remove(member);
            Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<TSelf>(document));
            document[member] = BsonNull.Value;
            Assert.Throws<BsonSerializationException>(() => BsonSerializer.Deserialize<TSelf>(document));
        }
    }

    public sealed class AddressDataList : List<AddressData?>;
    public sealed class AddressCollection : List<Address?>;
    public sealed class AddressListResource : List<AddressResource>;
    public sealed class DerivedListData
    {
        public AddressDataList? Addresses { get; set; }
        public string Label { get; set; } = "not a sequence";
    }
    public sealed class CustomSequenceData
    {
        public MarkerSequence<int, AddressData?>? Addresses { get; set; }
    }
    public sealed class SequenceResource
    {
        public List<AddressResource?>? Addresses { get; set; }
        public string? Label { get; set; }
    }
    public sealed class MarkerSequence<TMarker, TElement>(IEnumerable<TElement> elements) : IEnumerable<TElement>
    {
        public IEnumerator<TElement> GetEnumerator() => elements.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public sealed class AmbiguousSequence : IEnumerable<AddressData>, IEnumerable<string>
    {
        IEnumerator<AddressData> IEnumerable<AddressData>.GetEnumerator() => Enumerable.Empty<AddressData>().GetEnumerator();
        IEnumerator<string> IEnumerable<string>.GetEnumerator() => Enumerable.Empty<string>().GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<AddressData>)this).GetEnumerator();
    }
    public sealed class AdapterBookResource
    {
        public List<AdapterAddressResource>? Addresses { get; set; }
    }
    public sealed class AdapterAddressResource
    {
        public string? Line1 { get; set; }
    }
    public sealed class ConcurrentBookResource
    {
        public List<ConcurrentAddressResource>? Addresses { get; set; }
    }
    public sealed class ConcurrentBook : IComplexValueObject<ConcurrentBook, AddressBookData>
    {
        private readonly AddressBook _book;
        private ConcurrentBook(AddressBook book) { _book = book; }
        public AddressBookData ToData() => _book.ToData();
        public static ConcurrentBook Rehydrate(AddressBookData data) => new(AddressBook.Rehydrate(data));
        public static OneOf<ConcurrentBook, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, AddressBookData data) =>
            AddressBook.CreateMandatory(propertyName, data).Match<OneOf<ConcurrentBook, IEnumerable<DomainRuleFault>>>(
                book => new ConcurrentBook(book), faults => faults.ToList());
        public static OneOf<Maybe<ConcurrentBook>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, AddressBookData? data) =>
            data?.Addresses is null ? Maybe<ConcurrentBook>.None : CreateMandatory(propertyName, data).ToMaybe();
    }
    public sealed class ConcurrentAddressResource
    {
        public string? Line1 { get; set; }
    }
    public sealed class ComplexMember(EntityId<ComplexMember> id) : AggregateMember<ComplexMember>(id)
    {
        public Address? Address { get; set; }
        public AddressCollection? Addresses { get; set; }
        public AddressBook? Book { get; set; }
    }
    public sealed class ComplexChildResource
    {
        public AddressResource? Address { get; set; }
        public List<AddressResource?>? Addresses { get; set; }
        public AddressBookResource? Book { get; set; }
    }
}
