using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Infrastructure.Persistence;

/// <summary>An immutable, validated manual mapping descriptor, shared for the lifetime of configuration.</summary>
public abstract class AggregateRecordMapping
{
    private protected AggregateRecordMapping(Type aggregateType, Type recordType, IEnumerable<RecordPropertyPolicy>? policies)
    {
        AggregateType = aggregateType;
        Schema = RawRecordSchema.Create(aggregateType, recordType, policies);
        RequireMetadata("Id", typeof(Guid));
        RequireMetadata("TenantId", typeof(Guid));
        RequireMetadata("EntityTag", typeof(Guid));
        RequireMetadata("CreatedAction", typeof(UserActionRecord));
        RequireMetadata("UpdatedAction", typeof(UserActionRecord), optional: true);
        RequireMetadata("DeletedAction", typeof(UserActionRecord), optional: true);
    }

    public Type AggregateType { get; }

    public Type RecordType => Schema.RecordType;

    public RawRecordSchema Schema { get; }

    public abstract object Capture(object aggregate);

    public abstract object Restore(object record);

    private void RequireMetadata(string name, Type rawType, bool optional = false)
    {
        RawRecordProperty? property = Schema.Root.Properties.SingleOrDefault(property => property.Name == name);
        if (property is null || property.Value.Type != rawType ||
            (!optional && (property.AllowMissing || property.AllowNull)) ||
            (name == "Id" && property.StorageName != "_id"))
        {
            throw Schema.Failure(RecordType.Name + "." + name, "missing or incompatible raw aggregate metadata binding");
        }
    }
}

public sealed class AggregateRecordMapping<TAggregate, TRecord> : AggregateRecordMapping
    where TAggregate : AggregateRoot<TAggregate>
    where TRecord : class
{
    private readonly IAggregateRecordMapper<TAggregate, TRecord> _mapper;

    public AggregateRecordMapping(IAggregateRecordMapper<TAggregate, TRecord> mapper, IEnumerable<RecordPropertyPolicy>? policies = null)
        : base(typeof(TAggregate), typeof(TRecord), policies)
    {
        ArgumentNullException.ThrowIfNull(mapper);
        _mapper = mapper;
    }

    public TRecord Capture(TAggregate aggregate)
    {
        ArgumentNullException.ThrowIfNull(aggregate);
        return _mapper.Capture(aggregate) ?? throw Schema.Failure(RecordType.Name, "typed capture returned null");
    }

    public TAggregate Restore(TRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return _mapper.Restore(record) ?? throw Schema.Failure(RecordType.Name, "trusted restoration returned null");
    }

    public override object Capture(object aggregate) => Capture((TAggregate)aggregate);

    public override object Restore(object record) => Restore((TRecord)record);
}
