using System.Globalization;
using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Domain.ValueObjects.Abstractions;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;

namespace Synonms.Structur.Domain.Tests.Unit.Serialisation;

public class ValueObjectBsonSerialiserTests
{
    [Fact]
    public void TryRegisterSerialisersFrom_RegistersGenericValueObjectSerialisation()
    {
        BsonSerializer.TryRegisterSerializer(typeof(CustomOverrideValue), new CustomOverrideValueBsonSerialiser());

        StructurBsonSerialisation.TryRegisterSerialisersFrom(typeof(CustomOverrideValue).Assembly, typeof(AddressLine).Assembly);
        StructurBsonSerialisation.TryRegisterSerialisersFrom(typeof(CustomOverrideValue).Assembly, typeof(AddressLine).Assembly);

        Type[] valueObjectTypes = typeof(AddressLine).Assembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract && !type.ContainsGenericParameters)
            .Where(type => type.GetInterfaces().Any(contract => contract.IsGenericType
                                                                 && contract.GetGenericTypeDefinition() == typeof(IValueObject<,>)
                                                                 && contract.GetGenericArguments()[0] == type))
            .ToArray();

        Assert.NotEmpty(valueObjectTypes);

        foreach (Type type in valueObjectTypes)
        {
            Type contract = type.GetInterfaces().Single(interfaceType => interfaceType.IsGenericType
                                                                          && interfaceType.GetGenericTypeDefinition() == typeof(IValueObject<,>)
                                                                          && interfaceType.GetGenericArguments()[0] == type);
            Type valueType = contract.GetGenericArguments()[1];
            object value = Rehydrate(type, ExampleValue(valueType));
            IBsonSerializer serializer = BsonSerializer.LookupSerializer(type);

            Assert.Equal(type, serializer.ValueType);
            Assert.Equal(typeof(ValueObjectBsonSerialiser<,>).MakeGenericType(type, valueType), serializer.GetType());

            BsonDocument bsonDocument = SerializeDocument(type, value);
            BsonDocument scalarDocument = SerializeScalar(valueType, type.GetProperty("Value")!.GetValue(value)!);
            object? deserialized = DeserializeDocument(type, bsonDocument);
            object? expectedValue = DeserializeScalar(valueType, scalarDocument);
            object? actualValue = type.GetProperty("Value")!.GetValue(deserialized);

            Assert.Equal(expectedValue, actualValue);
            Assert.Equal(scalarDocument["Value"], bsonDocument["Value"]);

            BsonDocument nullValueDocument = SerializeDocument(type, null);
            Assert.Equal(BsonType.Null, nullValueDocument["Value"].BsonType);
            Assert.Null(DeserializeDocument(type, nullValueDocument));
        }

        FutureDecimal futureDecimal = FutureDecimal.Rehydrate(12.75m);
        Assert.IsType<ValueObjectBsonSerialiser<FutureDecimal, decimal>>(BsonSerializer.LookupSerializer<FutureDecimal>());
        BsonDocument futureDocument = SerializeDocument(typeof(FutureDecimal), futureDecimal);
        Assert.Equal(BsonType.Decimal128, futureDocument["Value"].BsonType);
        FutureDecimal? deserializedFuture = (FutureDecimal?)DeserializeDocument(typeof(FutureDecimal), futureDocument);
        Assert.Equal(futureDecimal.Value, deserializedFuture?.Value);

        BsonDocument nullDocument = SerializeDocument(typeof(FutureDecimal), null);
        Assert.Equal(BsonType.Null, nullDocument["Value"].BsonType);
        Assert.Null(DeserializeDocument(typeof(FutureDecimal), nullDocument));

        CustomOverrideValue customValue = CustomOverrideValue.Rehydrate(98.25m);
        Assert.IsType<CustomOverrideValueBsonSerialiser>(BsonSerializer.LookupSerializer<CustomOverrideValue>());
        BsonDocument customDocument = SerializeDocument(typeof(CustomOverrideValue), customValue);
        Assert.Equal(BsonType.String, customDocument["Value"].BsonType);
        Assert.Equal("custom:98.25", customDocument["Value"].AsString);
        CustomOverrideValue? deserializedCustom = (CustomOverrideValue?)DeserializeDocument(typeof(CustomOverrideValue), customDocument);
        Assert.Equal(customValue.Value, deserializedCustom?.Value);

        Guid identifier = Guid.NewGuid();
        EntityId<TestEntity> entityId = new(identifier);
        BsonDocument entityIdDocument = SerializeDocument(typeof(EntityId<TestEntity>), entityId);
        Assert.Equal(BsonType.Binary, entityIdDocument["Value"].BsonType);
        EntityId<TestEntity>? deserializedEntityId = (EntityId<TestEntity>?)DeserializeDocument(typeof(EntityId<TestEntity>), entityIdDocument);
        Assert.Equal(identifier, deserializedEntityId?.Value);

        EntityTag entityTag = identifier;
        BsonDocument entityTagDocument = SerializeDocument(typeof(EntityTag), entityTag);
        Assert.Equal(BsonType.Binary, entityTagDocument["Value"].BsonType);
        EntityTag? deserializedEntityTag = (EntityTag?)DeserializeDocument(typeof(EntityTag), entityTagDocument);
        Assert.Equal(identifier, deserializedEntityTag?.Value);

        AssertDateScalarSerialisation();
    }

    private static void AssertDateScalarSerialisation()
    {
        DateOnly dateOnly = new(2026, 4, 5);
        BirthDate birthDate = BirthDate.Rehydrate(dateOnly);
        BsonDocument dateDocument = SerializeDocument(typeof(BirthDate), birthDate);
        BsonDocument scalarDateDocument = SerializeScalar(typeof(DateOnly), dateOnly);
        Assert.Equal(scalarDateDocument["Value"], dateDocument["Value"]);
        BirthDate? rehydratedDate = (BirthDate?)DeserializeDocument(typeof(BirthDate), scalarDateDocument);
        Assert.Equal(dateOnly, rehydratedDate?.Value);

        foreach (DateTimeKind kind in new[] { DateTimeKind.Unspecified, DateTimeKind.Local, DateTimeKind.Utc })
        {
            DateTime eventDateTimeValue = new(2025, 9, 8, 7, 6, 5, kind);
            EventDateTime eventDateTime = EventDateTime.Rehydrate(eventDateTimeValue);
            BsonDocument eventDateTimeDocument = SerializeDocument(typeof(EventDateTime), eventDateTime);
            BsonDocument scalarDateTimeDocument = SerializeScalar(typeof(DateTime), eventDateTimeValue);
            Assert.Equal(scalarDateTimeDocument["Value"], eventDateTimeDocument["Value"]);
            EventDateTime? rehydratedEventDateTime = (EventDateTime?)DeserializeDocument(typeof(EventDateTime), scalarDateTimeDocument);
            DateTime expectedDateTime = (DateTime)DeserializeScalar(typeof(DateTime), scalarDateTimeDocument)!;
            Assert.Equal(expectedDateTime, rehydratedEventDateTime?.Value);
            Assert.Equal(expectedDateTime.Kind, rehydratedEventDateTime?.Value.Kind);
        }
    }

    private static BsonDocument SerializeScalar(Type valueType, object value)
    {
        BsonDocument document = new();
        using BsonDocumentWriter writer = new(document);
        writer.WriteStartDocument();
        writer.WriteName("Value");
        BsonSerializer.LookupSerializer(valueType).Serialize(
            BsonSerializationContext.CreateRoot(writer),
            new BsonSerializationArgs { NominalType = valueType }, value);
        writer.WriteEndDocument();
        return document;
    }

    private static object? DeserializeScalar(Type valueType, BsonDocument document)
    {
        using BsonDocumentReader reader = new(document);
        reader.ReadStartDocument();
        reader.ReadName();
        object? value = BsonSerializer.LookupSerializer(valueType).Deserialize(
            BsonDeserializationContext.CreateRoot(reader),
            new BsonDeserializationArgs { NominalType = valueType });
        reader.ReadEndDocument();
        return value;
    }

    private static object Rehydrate(Type type, object value) =>
        type.GetMethod("Rehydrate", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [value])!;

    private static object ExampleValue(Type valueType)
    {
        if (valueType == typeof(string))
        {
            return "example";
        }

        if (valueType == typeof(DateOnly))
        {
            return new DateOnly(2026, 4, 5);
        }

        if (valueType == typeof(DateTime))
        {
            return new DateTime(2026, 4, 5, 4, 3, 2, DateTimeKind.Utc);
        }

        if (valueType == typeof(int))
        {
            return 7;
        }

        throw new NotSupportedException($"No test value is defined for {valueType.Name}.");
    }

    private static BsonDocument SerializeDocument(Type valueObjectType, object? value)
    {
        MethodInfo method = typeof(ValueObjectBsonSerialiserTests).GetMethod(nameof(SerializeDocumentGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(valueObjectType);
        return (BsonDocument)method.Invoke(null, [value])!;
    }

    private static object? DeserializeDocument(Type valueObjectType, BsonDocument document)
    {
        MethodInfo method = typeof(ValueObjectBsonSerialiserTests).GetMethod(nameof(DeserializeDocumentGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(valueObjectType);
        return method.Invoke(null, [document]);
    }

    private static BsonDocument SerializeDocumentGeneric<TValueObject>(TValueObject? valueObject)
        where TValueObject : class =>
        new ValueObjectTestDocument<TValueObject> { Value = valueObject }.ToBsonDocument();

    private static TValueObject? DeserializeDocumentGeneric<TValueObject>(BsonDocument document)
        where TValueObject : class =>
        BsonSerializer.Deserialize<ValueObjectTestDocument<TValueObject>>(document.ToBson()).Value;

    public sealed class ValueObjectTestDocument<TValueObject>
        where TValueObject : class
    {
        public TValueObject? Value { get; set; }
    }

    public sealed class FutureDecimal : ValueObject<FutureDecimal, decimal>, IValueObject<FutureDecimal, decimal>
    {
        private FutureDecimal(decimal value) => Value = value;

        public override decimal Value { get; }

        public static implicit operator decimal(FutureDecimal valueObject) => valueObject.Value;

        public static OneOf<FutureDecimal, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, decimal value) =>
            new FutureDecimal(value);

        public static OneOf<Maybe<FutureDecimal>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, decimal value) =>
            CreateMandatory(propertyName, value).ToMaybe();

        public static FutureDecimal Rehydrate(decimal value) => new(value);
    }

    public sealed class CustomOverrideValue : ValueObject<CustomOverrideValue, decimal>, IValueObject<CustomOverrideValue, decimal>
    {
        private CustomOverrideValue(decimal value) => Value = value;

        public override decimal Value { get; }

        public static implicit operator decimal(CustomOverrideValue valueObject) => valueObject.Value;

        public static OneOf<CustomOverrideValue, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, decimal value) =>
            new CustomOverrideValue(value);

        public static OneOf<Maybe<CustomOverrideValue>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, decimal value) =>
            CreateMandatory(propertyName, value).ToMaybe();

        public static CustomOverrideValue Rehydrate(decimal value) => new(value);
    }

    public sealed class CustomOverrideValueBsonSerialiser : SerializerBase<CustomOverrideValue?>
    {
        public override CustomOverrideValue? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            if (context.Reader.CurrentBsonType is BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }

            string value = context.Reader.ReadString();
            return CustomOverrideValue.Rehydrate(decimal.Parse(value["custom:".Length..], CultureInfo.InvariantCulture));
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, CustomOverrideValue? value)
        {
            if (value is null)
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteString($"custom:{value.Value.ToString(CultureInfo.InvariantCulture)}");
        }
    }

    private sealed class TestEntity : Entity<TestEntity> { }
}
