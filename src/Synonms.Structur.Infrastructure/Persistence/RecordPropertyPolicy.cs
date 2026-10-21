namespace Synonms.Structur.Infrastructure.Persistence;

/// <summary>
/// Explicit storage naming and independent absence/null policies. Missing optional fields decode as
/// null, never as a DTO's constructor/initializer default. Required fields reject both by default.
/// Policies apply to the named CLR property on a concrete record type, including inherited properties.
/// </summary>
public sealed record RecordPropertyPolicy(
    Type RecordType,
    string PropertyName,
    string StorageName,
    bool AllowMissing = false,
    bool AllowNull = false);
