namespace Synonms.Structur.Domain.Persistence;

/// <summary>Opts a concrete aggregate or owned member into deterministic plain-record generation.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class GenerateStorageAttribute(string schemaName, int schemaVersion = 1) : Attribute
{
    public string SchemaName { get; } = schemaName;

    public int SchemaVersion { get; } = schemaVersion;
}

/// <summary>Explicitly classifies a persisted property and assigns its stable BSON field name.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class StoredStateAttribute(string storageName) : Attribute
{
    public string StorageName { get; } = storageName;
}

/// <summary>Overrides the default nullable-state missing and BSON-null policies independently.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class StorageNullPolicyAttribute(bool allowMissing, bool allowNull) : Attribute
{
    public bool AllowMissing { get; } = allowMissing;

    public bool AllowNull { get; } = allowNull;
}

/// <summary>Explicitly excludes computed or transient state from a generated storage schema.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class NotStoredAttribute : Attribute;

/// <summary>Declares a custom, typed domain-to-raw codec for a property unsupported by the built-in scalar inventory.</summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class StorageOverrideAttribute(Type rawType, Type codecType) : Attribute
{
    public Type RawType { get; } = rawType;

    public Type CodecType { get; } = codecType;
}

/// <summary>A generated/manual conversion hook for a single declared storage property.</summary>
public interface IStorageValueCodec<TDomain, TRaw>
{
    TRaw Capture(TDomain value);

    TDomain Restore(TRaw value);
}

/// <summary>Shares a stateless, thread-safe storage codec instance for generated typed bridges.</summary>
public static class StorageValueCodecCache<TCodec>
    where TCodec : class, new()
{
    public static TCodec Instance { get; } = new();
}
