using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Tests.Integration;

// Demonstrates the noncanonical persistence escape hatch without altering any built-in serializer.
public sealed class AdaptedAddress : IComplexValueObject<AdaptedAddress, AddressData>
{
    private AdaptedAddress(Address address) => Address = address;
    public Address Address { get; }
    public AddressData ToData() => Address.ToData();
    public static AdaptedAddress Rehydrate(AddressData data) => new(Address.Rehydrate(data));
    public static OneOf<AdaptedAddress, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, AddressData data) =>
        Address.CreateMandatory(propertyName, data).Match<OneOf<AdaptedAddress, IEnumerable<DomainRuleFault>>>(
            address => new AdaptedAddress(address), faults => faults.ToList());
    public static OneOf<Maybe<AdaptedAddress>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, AddressData? data) =>
        data is null ? Maybe<AdaptedAddress>.None : CreateMandatory(propertyName, data).ToMaybe();
}

public sealed class AdaptedAddressBsonSerialiser : SerializerBase<AdaptedAddress>
{
    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, AdaptedAddress value) =>
        context.Writer.WriteString(value.Address.Line1.Value);

    public override AdaptedAddress Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args) =>
        AdaptedAddress.Rehydrate(new() { Type = "Home", Line1 = context.Reader.ReadString(), Postcode = "SW1A 1AA" });
}

public sealed class AdaptedRecord
{
    public AdaptedAddress? Address { get; set; }
}

public sealed class AdaptedResource
{
    public string? Street { get; init; }
}
