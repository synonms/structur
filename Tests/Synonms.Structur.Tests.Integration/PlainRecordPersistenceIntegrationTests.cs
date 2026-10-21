using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Synonms.Structur.Api.Server.Tenants.Context;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Infrastructure.MongoDb.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Infrastructure.MongoDb.Serialisation;
using Synonms.Structur.Infrastructure.Persistence;

namespace Synonms.Structur.Tests.Integration;

/// <summary>Driver-only integration of domain restoration, mapping, raw schema and BSON; no Mongo server.</summary>
public class PlainRecordPersistenceIntegrationTests
{
    private static readonly Guid RootId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TenantId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Tag = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly Guid MemberId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTime Timestamp = new(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);

    private static AggregateRecordMapping<FixtureRoot, RootRecord> Mapping() => new(new ManualMapper(),
    [
        new(typeof(EmailContactData), nameof(EmailContactData.Type), "Type"),
        new(typeof(EmailContactData), nameof(EmailContactData.Address), "Address"),
        new(typeof(EmailContactData), nameof(EmailContactData.IsPrimary), "IsPrimary")
    ]);

    private static MongoDatabaseConfiguration Configuration(AggregateRecordMapping? mapping = null) =>
        new("fixture", new Dictionary<Type, string> { [typeof(FixtureRoot)] = "roots" }, [mapping ?? Mapping()]);

    [Fact]
    public void QueryTranslatorTargetsRawNestedFieldsAndPreservesSameElementListSemantics()
    {
        MongoDbAggregateRecordMapper<FixtureRoot> mapper = new(Configuration());
        MongoDbAggregateQueryTranslator<FixtureRoot> translator = new(mapper);

        AggregateQuery<FixtureRoot> query = AggregateQuery<FixtureRoot>.All
            .Where(root => root.Email.Address.Value == "member@example.test" &&
                           root.Members.Any(member => member.Name.Value == "primary" && member.Labels.Contains("active")))
            .OrderBy(root => root.Email.Address.Value, descending: true);

        Assert.Equal(new BsonDocument
        {
            {
                "$and", new BsonArray
                {
                    new BsonDocument("Email.Address", new BsonDocument("$eq", "member@example.test")),
                    new BsonDocument("Members", new BsonDocument("$elemMatch", new BsonDocument("$and", new BsonArray
                    {
                        new BsonDocument("Name", new BsonDocument("$eq", "primary")),
                        new BsonDocument("Labels", new BsonDocument("$eq", "active"))
                    })))
                }
            }
        }, translator.Predicate(query));
        Assert.Equal(new BsonDocument
        {
            { "Email.Address", -1 },
            { "_id", 1 }
        }, translator.Sort(query));
    }

    [Fact]
    public void QueryTranslatorRejectsUnmappedComputedFieldsBeforeExecution()
    {
        MongoDbAggregateRecordMapper<FixtureRoot> mapper = new(Configuration());
        MongoDbAggregateQueryTranslator<FixtureRoot> translator = new(mapper);
        AggregateQuery<FixtureRoot> query = AggregateQuery<FixtureRoot>.All
            .Where(root => root.CommandOnlyComputed == "not translated");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => translator.Predicate(query));
        Assert.Contains("unmapped field 'CommandOnlyComputed'", exception.Message, StringComparison.Ordinal);
    }

    private static RootRecord StoredRecord() => new()
    {
        Id = RootId,
        TenantId = TenantId,
        EntityTag = Tag,
        CreatedAction = new() { ActionAt = Timestamp, ActionById = Guid.Empty, ActionByName = "" },
        UpdatedAction = new() { ActionAt = Timestamp.AddMinutes(1), ActionById = RootId, ActionByName = "historical" },
        DeletedAction = new() { ActionAt = Timestamp.AddMinutes(2), ActionById = TenantId, ActionByName = "deleted" },
        Email = new() { Type = "Personal", Address = "representable-but-invalid", IsPrimary = false },
        Members = [new() { Id = MemberId, Name = "", Labels = ["one", "two"] }]
    };

    [Fact]
    public void TypedManualBoundaryCopiesBothDirectionsAndPreservesTrustedRootMemberAndAuditState()
    {
        AggregateRecordMapping<FixtureRoot, RootRecord> mapping = Mapping();
        RootRecord stored = StoredRecord();
        FixtureRoot restored = mapping.Restore(stored);
        RootRecord captured = mapping.Capture(restored);

        Assert.Equal(RootId, restored.Id.Value);
        Assert.Equal(TenantId, restored.TenantId);
        Assert.Equal(Tag, restored.EntityTag.Value);
        Assert.Equal(MemberId, restored.Members[0].Id.Value);
        Assert.Equal("", restored.Members[0].Name.Value);
        Assert.Equal("representable-but-invalid", restored.Email.Address.Value);
        Assert.Equal(stored.CreatedAction.ActionAt, restored.CreatedAction.ActionAt);
        Assert.Equal(stored.CreatedAction.ActionById, restored.CreatedAction.ActionById);
        Assert.Equal(stored.CreatedAction.ActionByName, restored.CreatedAction.ActionByName);
        Assert.Equal(stored.UpdatedAction!.ActionByName, restored.UpdatedAction!.ActionByName);
        Assert.Equal(stored.DeletedAction!.ActionAt, restored.DeletedAction!.ActionAt);
        Assert.NotSame(stored.Members, captured.Members);
        Assert.NotSame(stored.Members[0], captured.Members[0]);
        Assert.NotSame(stored.Members[0].Labels, captured.Members[0].Labels);
        Assert.NotSame(stored.CreatedAction, captured.CreatedAction);
        Assert.False(UserAction.CreateMandatory("CreatedAction", new UserActionDto
        {
            ActionAt = restored.CreatedAction.ActionAt,
            ActionById = restored.CreatedAction.ActionById,
            ActionByName = restored.CreatedAction.ActionByName
        }).Match(_ => true, _ => false));

        stored.Members[0].Labels.Add("mutated input");
        stored.Members.Clear();
        stored.CreatedAction.ActionByName = "mutated audit";
        restored.Members[0].Labels.Add("mutated domain");
        Assert.Equal(["one", "two"], captured.Members[0].Labels);
        captured.Members[0].Labels.Add("mutated capture");
        Assert.Equal(["one", "two", "mutated domain"], restored.Members[0].Labels);
        Assert.Equal("", restored.CreatedAction.ActionByName);
        Assert.Equal(Tag, restored.EntityTag.Value);
    }

    [Fact]
    public void NormalPathUsesPlainRecordBsonWithoutAggregateOrValueObjectSerializerRegistration()
    {
        // No StructurBsonSerialisation call: the domain's computed property would throw if
        // the mapper attempted aggregate -> BSON -> record conversion.
        MongoDbAggregateRecordMapper<FixtureRoot> adapter = new(Configuration());
        FixtureRoot root = Mapping().Restore(StoredRecord());
        BsonDocument document = adapter.Capture(root);
        Assert.False(document.Contains("_t"));
        Assert.Equal(BsonBinarySubType.UuidStandard, document["_id"].AsBsonBinaryData.SubType);
        Assert.Equal(RootId, document["_id"].AsBsonBinaryData.ToGuid());
        Assert.Equal(Tag, document["EntityTag"].AsBsonBinaryData.ToGuid());
        Assert.Equal(BsonType.String, document["Email"]["Type"].BsonType);
        Assert.Equal("representable-but-invalid", document["Email"]["Address"].AsString);
        Assert.Equal(MemberId, document["Members"][0]["_id"].AsBsonBinaryData.ToGuid());
        Assert.Equal(new BsonArray { "one", "two" }, document["Members"][0]["Labels"]);
        Assert.Equal(new BsonDocument
        {
            { "ActionAt", new BsonDateTime(Timestamp) },
            { "ActionById", new BsonBinaryData(Guid.Empty, GuidRepresentation.Standard) },
            { "ActionByName", "" }
        }, document["CreatedAction"]);
        FixtureRoot restored = adapter.Restore(document);
        Assert.Equal(root.Id, restored.Id);
        Assert.Equal(root.EntityTag, restored.EntityTag);
        Assert.Equal(document, adapter.Capture(restored));
    }

    [Theory]
    [InlineData("_id")]
    [InlineData("TenantId")]
    [InlineData("EntityTag")]
    [InlineData("CreatedAction")]
    public void MissingOrNullSecurityAndAuditMetadataFailsInsteadOfInventingState(string name)
    {
        MongoDbAggregateRecordMapper<FixtureRoot> adapter = new(Configuration());
        BsonDocument document = adapter.Capture(Mapping().Restore(StoredRecord()));
        document.Remove(name);
        Assert.Contains("missing", Assert.Throws<InvalidOperationException>(() => adapter.Restore(document)).Message);
        document[name] = BsonNull.Value;
        Assert.Contains("null", Assert.Throws<InvalidOperationException>(() => adapter.Restore(document)).Message);
    }

    [Fact]
    public void PresenceAndNullPoliciesAreIndependentAndConstructorDefaultsAreNotMissingPolicies()
    {
        RawRecordSchema schema = RawRecordSchema.Create(typeof(FixtureRoot), typeof(PresenceRecord),
        [
            new(typeof(PresenceRecord), nameof(PresenceRecord.Value), "historicalName", AllowMissing: true, AllowNull: false)
        ]);
        RawRecordBsonCodec codec = new(schema);
        PresenceRecord missing = (PresenceRecord)codec.Decode(new());
        Assert.Null(missing.Value);
        Assert.Throws<InvalidOperationException>(() => codec.Decode(new("historicalName", BsonNull.Value)));
        Assert.Equal("stored", ((PresenceRecord)codec.Decode(new("historicalName", "stored"))).Value);
        Assert.Equal(new BsonDocument("historicalName", "stored"), codec.Encode(new PresenceRecord { Value = "stored" }));
    }

    [Fact]
    public void NestedMissingMalformedAndPolymorphicDataFailWithValueFreePropertyDiagnostics()
    {
        MongoDbAggregateRecordMapper<FixtureRoot> adapter = new(Configuration());
        BsonDocument document = adapter.Capture(Mapping().Restore(StoredRecord()));
        document["Email"].AsBsonDocument.Remove("Address");
        Assert.Contains("RootRecord.Email.Address", Assert.Throws<InvalidOperationException>(() => adapter.Restore(document)).Message);
        document["Email"]["Address"] = "sensitive persisted value";
        document["EntityTag"] = "sensitive persisted value";
        InvalidOperationException malformed = Assert.Throws<InvalidOperationException>(() => adapter.Restore(document));
        Assert.Contains("RootRecord.EntityTag", malformed.Message);
        Assert.DoesNotContain("sensitive persisted value", malformed.ToString());
        document["EntityTag"] = new BsonBinaryData(Tag, GuidRepresentation.Standard);
        document["Members"][0]["_t"] = "DerivedMember";
        Assert.Contains("polymorphic", Assert.Throws<InvalidOperationException>(() => adapter.Restore(document)).Message);
        document["Members"][0].AsBsonDocument.Remove("_t");
        document["CreatedAction"].AsBsonDocument.Remove("ActionById");
        Assert.Contains("CreatedAction.ActionById", Assert.Throws<InvalidOperationException>(() => adapter.Restore(document)).Message);
    }

    [Fact]
    public void HandwrittenRestorationAndDtoAccessorFailuresCannotExposePersistedValues()
    {
        AggregateRecordMapping<FixtureRoot, RootRecord> mapping = new(new ValueLeakingMapper());
        MongoDbAggregateRecordMapper<FixtureRoot> adapter = new(Configuration(mapping));
        BsonDocument document = new RawRecordBsonCodec(mapping.Schema).Encode(StoredRecord());
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => adapter.Restore(document));
        Assert.Contains("FixtureRoot/RootRecord", failure.Message);
        Assert.DoesNotContain("representable-but-invalid", failure.ToString());
        RawRecordBsonCodec codec = new(RawRecordSchema.Create(typeof(FixtureRoot), typeof(AccessorRecord)));
        InvalidOperationException setterFailure = Assert.Throws<InvalidOperationException>(() => codec.Decode(new("Value", "sensitive input")));
        Assert.Contains("AccessorRecord.Value", setterFailure.Message);
        Assert.DoesNotContain("sensitive input", setterFailure.ToString());
    }

    [Fact]
    public void RegistrationRequiresExplicitMatchingPairAndRejectsConflictsAndMutation()
    {
        Dictionary<Type, string> collections = new() { [typeof(FixtureRoot)] = "roots" };
        Assert.Contains("missing typed", Assert.Throws<InvalidOperationException>(() => new MongoDatabaseConfiguration("fixture", collections)).Message);
        AggregateRecordMapping mapping = Mapping();
        string conflict = Assert.Throws<InvalidOperationException>(() => new MongoDatabaseConfiguration("fixture", collections, [mapping, Mapping()])).Message;
        Assert.Contains("conflicting", conflict);
        Assert.Equal(conflict, Assert.Throws<InvalidOperationException>(() => new MongoDatabaseConfiguration("fixture", collections, [Mapping(), mapping])).Message);
#pragma warning disable CS0618
        Assert.Throws<InvalidOperationException>(() => new MongoDatabaseConfiguration("fixture",
            new Dictionary<Type, AggregatePersistenceConfiguration> { [typeof(FixtureRoot)] = new("roots", typeof(RootRecord)) }));
        MongoDatabaseConfiguration legacy = MongoDatabaseConfiguration.UseLegacyDirectAggregatePersistence("fixture", collections);
#pragma warning restore CS0618
        Assert.True(legacy.IsLegacyDirectAggregatePersistence);
        Assert.Equal(typeof(FixtureRoot), legacy.GetRecordType<FixtureRoot>());
        MongoDatabaseConfiguration normal = new("fixture", collections, [mapping]);
        collections.Clear();
        Assert.Equal("roots", normal.GetCollectionName<FixtureRoot>());
        Assert.Same(mapping, normal.GetPersistenceConfiguration(typeof(FixtureRoot)).Mapping);
        Assert.Throws<NotSupportedException>(() => normal.CollectionNamesByAggregateType.Clear());
        Assert.Throws<NotSupportedException>(() => normal.PersistenceConfigurationsByAggregateType.Clear());
    }

    [Theory]
    [InlineData(typeof(Envelope<Moniker>))]
    [InlineData(typeof(Envelope<EmailContact>))]
    [InlineData(typeof(Envelope<EntityId<FixtureRoot>>))]
    [InlineData(typeof(Envelope<EntityTag>))]
    [InlineData(typeof(Envelope<UserAction>))]
    [InlineData(typeof(Envelope<FixtureRoot>))]
    [InlineData(typeof(Envelope<FixtureMember>))]
    [InlineData(typeof(Envelope<object>))]
    [InlineData(typeof(Envelope<Action>))]
    [InlineData(typeof(Envelope<Dictionary<string, int>>))]
    [InlineData(typeof(Envelope<IReadOnlyList<int>>))]
    [InlineData(typeof(Envelope<HashSet<int>>))]
    [InlineData(typeof(Envelope<byte[]>))]
    [InlineData(typeof(Envelope<DayOfWeek>))]
    [InlineData(typeof(Envelope<BsonDocument>))]
    [InlineData(typeof(Envelope<IDisposable>))]
    [InlineData(typeof(Envelope<CyclicRecord>))]
    public void ManualGraphValidationRejectsForbiddenNestedGraphTypes(Type type)
    {
        InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => RawRecordSchema.Create(typeof(FixtureRoot), type));
        Assert.Contains("Nested", failure.Message);
        Assert.Contains("FixtureRoot", failure.Message);
    }

    [Fact]
    public void ValidationCoversInheritedCollisionsInaccessibleBindingsAndDomainOwnedRawDtos()
    {
        Assert.Throws<InvalidOperationException>(() => RawRecordSchema.Create(typeof(FixtureRoot), typeof(HiddenNameRecord)));
        Assert.Throws<InvalidOperationException>(() => RawRecordSchema.Create(typeof(FixtureRoot), typeof(InaccessibleRecord)));
        Assert.Throws<InvalidOperationException>(() => RawRecordSchema.Create(typeof(FixtureRoot), typeof(PresenceRecord),
            [new(typeof(PresenceRecord), "DoesNotExist", "stored")]));
        Assert.Throws<InvalidOperationException>(() => RawRecordSchema.Create(typeof(FixtureRoot), typeof(PresenceRecord),
            [new(typeof(PresenceRecord), "Value", "x"), new(typeof(PresenceRecord), "Value", "y")]));
        Assert.Throws<InvalidOperationException>(() => RawRecordSchema.Create(typeof(FixtureRoot), typeof(MemberRecord),
            [new(typeof(MemberRecord), "Name", "Labels")]));
        RawRecordSchema dto = RawRecordSchema.Create(typeof(FixtureRoot), typeof(AddressData));
        Assert.Equal(typeof(AddressData), dto.RecordType);
        RawRecordBsonCodec codec = new(RawRecordSchema.Create(typeof(FixtureRoot), typeof(PresenceRecord)));
        Assert.Contains("polymorphism", Assert.Throws<InvalidOperationException>(() => codec.Encode(new HiddenNameRecord())).Message);
    }

    [Fact]
    public void CodecInventoryPinsRawWidthsPrecisionDateAndNullShapes()
    {
        RawRecordBsonCodec codec = new(RawRecordSchema.Create(typeof(FixtureRoot), typeof(CodecRecord)));
        CodecRecord record = new() { Integer = 42, Long = 43L, Decimal = 1234.567890123456789m, Double = 1.5,
            Date = new(2026, 1, 2), Timestamp = Timestamp, Optional = null };
        BsonDocument expected = new()
        {
            { "Date", "2026-01-02" },
            { "Decimal", new BsonDecimal128(record.Decimal) },
            { "Double", new BsonDouble(1.5) },
            { "Integer", new BsonInt32(42) },
            { "Long", new BsonInt64(43L) },
            { "Optional", BsonNull.Value },
            { "Timestamp", new BsonDateTime(Timestamp) }
        };
        Assert.Equal(expected, codec.Encode(record));
        Assert.Equal(expected, codec.Encode(codec.Decode(expected)));
        expected["Long"] = new BsonInt32(43);
        Assert.Throws<InvalidOperationException>(() => codec.Decode(expected));
        record.Timestamp = DateTime.SpecifyKind(Timestamp, DateTimeKind.Unspecified);
        Assert.Throws<InvalidOperationException>(() => codec.Encode(record));
    }

    [Fact]
    public void ActualLegacyBsonDecodesThroughRawRecordsAndNewBsonRemainsLegacyCompatible()
    {
        // Legacy serializers are used to capture the old fixture, never by the normal adapter.
        BsonSerializer.TryRegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        StructurBsonSerialisation.TryRegisterSerialisersFrom();
        LegacyRoot original = new(RootId, TenantId, Tag, UserAction.Rehydrate(new UserActionDto
        {
            ActionAt = Timestamp, ActionById = Guid.Empty, ActionByName = ""
        })) { Name = Moniker.Rehydrate(""), Count = -1 };
#pragma warning disable CS0618
        MongoDatabaseConfiguration legacyConfiguration = MongoDatabaseConfiguration.UseLegacyDirectAggregatePersistence("fixture",
            new Dictionary<Type, string> { [typeof(LegacyRoot)] = "roots" });
#pragma warning restore CS0618
        MongoDbAggregateRecordMapper<LegacyRoot> legacy = new(legacyConfiguration);
        BsonDocument oldDocument = legacy.Capture(original);
        MongoDatabaseConfiguration normalConfiguration = new("fixture",
            new Dictionary<Type, string> { [typeof(LegacyRoot)] = "roots" },
            [new AggregateRecordMapping<LegacyRoot, ParityRecord>(new ParityMapper())]);
        MongoDbAggregateRecordMapper<LegacyRoot> normal = new(normalConfiguration);
        LegacyRoot restored = normal.Restore(oldDocument);
        Assert.Equal(original.Id, restored.Id);
        Assert.Equal(original.EntityTag, restored.EntityTag);
        Assert.Equal("", restored.Name!.Value);
        Assert.Equal(-1, restored.Count);
        // Field order is not contractual for this fixture. Names, types, omitted/null shape,
        // UUID representation, audit timestamp and scalar representations are contractual.
        Assert.Equal(Canonical(oldDocument), Canonical(normal.Capture(restored)));
        Assert.Equal(Canonical(oldDocument), Canonical(legacy.Capture(restored)));
    }

    [Fact]
    public void RepositoryScopeFiltersUseExplicitSchemaStorageNamesWithoutVoSerializers()
    {
        AggregateRecordMapping<FixtureRoot, RootRecord> mapping = new(new ManualMapper(),
        [
            new(typeof(RootRecord), nameof(RootRecord.TenantId), "tenant"),
            new(typeof(RootRecord), nameof(RootRecord.DeletedAction), "removed", AllowMissing: true, AllowNull: true)
        ]);
        MongoDatabaseConfiguration configuration = Configuration(mapping);
        MongoClient client = new("mongodb://localhost:27017"); // Constructing repositories does not issue commands.
        MongoDbMultiTenantReadAggregateRepository<FixtureRoot> repository = new(new FixtureTenantContext(), client, configuration);
        BsonDocument filter = repository.GlobalFilter.Render(new RenderArgs<BsonDocument>(
            BsonDocumentSerializer.Instance, BsonSerializer.SerializerRegistry));
        Assert.Equal(new BsonDocument
        {
            { "removed", BsonNull.Value },
            { "tenant", new BsonBinaryData(TenantId, GuidRepresentation.Standard) }
        }, filter);
    }

    private static BsonValue Canonical(BsonValue value) => value switch
    {
        BsonDocument document => new BsonDocument(document.Elements.OrderBy(element => element.Name, StringComparer.Ordinal)
            .Select(element => new BsonElement(element.Name, Canonical(element.Value)))),
        BsonArray array => new BsonArray(array.Select(Canonical)),
        _ => value
    };

    public sealed class FixtureRoot : AggregateRoot<FixtureRoot>
    {
        private FixtureRoot(RootRecord record) : base(new EntityId<FixtureRoot>(record.Id), record.TenantId,
            new EntityTag(record.EntityTag), RestoreAction(record.CreatedAction), RestoreOptional(record.UpdatedAction), RestoreOptional(record.DeletedAction))
        {
            Email = EmailContact.Rehydrate(record.Email);
            Members = record.Members.Select(FixtureMember.Restore).ToList();
        }

        public EmailContact Email { get; }
        public List<FixtureMember> Members { get; }
        public string CommandOnlyComputed => throw new InvalidOperationException("Aggregate BSON serialization is forbidden.");
        public static FixtureRoot Restore(RootRecord record) => new(record);
        private static UserAction? RestoreOptional(UserActionRecord? record) => record is null ? null : RestoreAction(record);
        private static UserAction RestoreAction(UserActionRecord record) => UserAction.Rehydrate(new UserActionDto
        {
            ActionAt = record.ActionAt, ActionById = record.ActionById, ActionByName = record.ActionByName
        });
    }

    public sealed class FixtureMember : AggregateMember<FixtureMember>
    {
        private FixtureMember(MemberRecord record) : base(new EntityId<FixtureMember>(record.Id))
        {
            Name = Moniker.Rehydrate(record.Name);
            Labels = record.Labels.ToList();
        }

        public Moniker Name { get; }
        public List<string> Labels { get; }
        public static FixtureMember Restore(MemberRecord record) => new(record);
    }

    public sealed class RootRecord : AggregateRootRecord<FixtureRoot>
    {
        public EmailContactData Email { get; set; } = new();
        public List<MemberRecord> Members { get; set; } = [];
    }

    public sealed class MemberRecord : AggregateMemberRecord<FixtureMember>
    {
        public string Name { get; set; } = "";
        public List<string> Labels { get; set; } = [];
    }

    private sealed class ManualMapper : IAggregateRecordMapper<FixtureRoot, RootRecord>
    {
        public FixtureRoot Restore(RootRecord record) => FixtureRoot.Restore(record);

        public RootRecord Capture(FixtureRoot aggregate) => new()
        {
            Id = aggregate.Id.Value,
            TenantId = aggregate.TenantId,
            EntityTag = aggregate.EntityTag.Value,
            CreatedAction = CaptureAction(aggregate.CreatedAction),
            UpdatedAction = aggregate.UpdatedAction is null ? null : CaptureAction(aggregate.UpdatedAction),
            DeletedAction = aggregate.DeletedAction is null ? null : CaptureAction(aggregate.DeletedAction),
            Email = aggregate.Email.ToData(),
            Members = aggregate.Members.Select(member => new MemberRecord
            {
                Id = member.Id.Value, Name = member.Name.Value, Labels = member.Labels.ToList()
            }).ToList()
        };

        private static UserActionRecord CaptureAction(UserAction action) => new()
        {
            ActionAt = action.ActionAt, ActionById = action.ActionById, ActionByName = action.ActionByName
        };
    }

    private sealed class ValueLeakingMapper : IAggregateRecordMapper<FixtureRoot, RootRecord>
    {
        public RootRecord Capture(FixtureRoot aggregate) => new ManualMapper().Capture(aggregate);
        public FixtureRoot Restore(RootRecord record) => throw new FormatException(record.Email.Address);
    }

    public sealed class AccessorRecord
    {
        public string Value
        {
            get => "";
            set => throw new FormatException(value);
        }
    }

    public sealed class Envelope<T>
    {
        public List<NestedRecord<T>> Nested { get; set; } = [];
    }

    public sealed class NestedRecord<T>
    {
        public T? Value { get; set; }
    }

    public sealed class CyclicRecord
    {
        public CyclicRecord? Next { get; set; }
    }

    public class PresenceRecord
    {
        public string? Value { get; set; } = "constructor default must not become storage policy";
    }

    public sealed class HiddenNameRecord : PresenceRecord
    {
        public new string? Value { get; set; }
    }

    public sealed class InaccessibleRecord
    {
        public string Value { get; private set; } = "";
    }

    public sealed class CodecRecord
    {
        public int Integer { get; set; }
        public long Long { get; set; }
        public decimal Decimal { get; set; }
        public double Double { get; set; }
        public DateOnly Date { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Optional { get; set; }
    }

    public sealed class LegacyRoot : AggregateRoot<LegacyRoot>
    {
        public LegacyRoot(Guid id, Guid tenant, Guid tag, UserAction created, UserAction? updated = null, UserAction? deleted = null)
            : base(new EntityId<LegacyRoot>(id), tenant, new EntityTag(tag), created, updated, deleted) { }

        public Moniker? Name { get; set; }
        public int Count { get; set; }
    }

    public sealed class ParityRecord : AggregateRootRecord<LegacyRoot>
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    private sealed class ParityMapper : IAggregateRecordMapper<LegacyRoot, ParityRecord>
    {
        public ParityRecord Capture(LegacyRoot aggregate) => new()
        {
            Id = aggregate.Id.Value, TenantId = aggregate.TenantId, EntityTag = aggregate.EntityTag.Value,
            CreatedAction = CaptureAction(aggregate.CreatedAction),
            UpdatedAction = aggregate.UpdatedAction is null ? null : CaptureAction(aggregate.UpdatedAction),
            DeletedAction = aggregate.DeletedAction is null ? null : CaptureAction(aggregate.DeletedAction),
            Name = aggregate.Name?.Value, Count = aggregate.Count
        };

        public LegacyRoot Restore(ParityRecord record) => new(record.Id, record.TenantId, record.EntityTag,
            RestoreAction(record.CreatedAction),
            record.UpdatedAction is null ? null : RestoreAction(record.UpdatedAction),
            record.DeletedAction is null ? null : RestoreAction(record.DeletedAction))
        {
            Name = record.Name is null ? null : Moniker.Rehydrate(record.Name), Count = record.Count
        };

        private static UserActionRecord CaptureAction(UserAction action) => new()
        {
            ActionAt = action.ActionAt, ActionById = action.ActionById, ActionByName = action.ActionByName
        };

        private static UserAction RestoreAction(UserActionRecord action) => UserAction.Rehydrate(new UserActionDto
        {
            ActionAt = action.ActionAt, ActionById = action.ActionById, ActionByName = action.ActionByName
        });
    }

    private sealed class FixtureTenantContext : ITenantContext
    {
        public Guid? GetTenantId() => TenantId;
        public bool HasTenantId() => true;
    }
}
