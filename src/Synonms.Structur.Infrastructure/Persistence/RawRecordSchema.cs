using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Synonms.Structur.Infrastructure.Persistence;

/// <summary>
/// Validates an acyclic raw graph once and caches its bindings. No BSON attributes or per-row
/// reflection are needed. Supported collections are owned List&lt;T&gt; only; enums must be mapped
/// explicitly to a raw integer/string. Binary and custom scalar codecs are not supported yet.
/// </summary>
public sealed class RawRecordSchema
{
    private RawRecordSchema(Type aggregateType, RawRecordNode root)
    {
        AggregateType = aggregateType;
        Root = root;
    }

    public Type AggregateType { get; }
    public Type RecordType => Root.Type;
    public RawRecordNode Root { get; }

    public static RawRecordSchema Create(Type aggregateType, Type recordType, IEnumerable<RecordPropertyPolicy>? policies = null)
    {
        Dictionary<(Type, string), RecordPropertyPolicy> overrides = new();
        foreach (RecordPropertyPolicy policy in policies ?? [])
        {
            if (!overrides.TryAdd((policy.RecordType, policy.PropertyName), policy))
            {
                throw Error(aggregateType, recordType, policy.PropertyName, "conflicting property policies");
            }
        }

        HashSet<(Type, string)> used = [];
        RawRecordNode root = Build(aggregateType, recordType, recordType, recordType.Name, [], overrides, used);
        if (root.Properties.Count == 0)
        {
            throw Error(aggregateType, recordType, recordType.Name, "root must be a concrete record DTO");
        }

        foreach ((Type type, string name) in overrides.Keys.Except(used))
        {
            throw Error(aggregateType, recordType, $"{type.Name}.{name}", "policy has no accessible binding");
        }

        return new RawRecordSchema(aggregateType, root);
    }

    public InvalidOperationException Failure(string path, string reason) =>
        Error(AggregateType, RecordType, path, reason);

    private static RawRecordNode Build(Type aggregate, Type record, Type type, string path, HashSet<Type> ancestors,
        Dictionary<(Type, string), RecordPropertyPolicy> overrides, HashSet<(Type, string)> used)
    {
        Type? underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null)
        {
            return new RawRecordNode(type, element: Build(aggregate, record, underlying, path, ancestors, overrides, used));
        }

        if (IsScalar(type))
        {
            return new RawRecordNode(type);
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            if (!ancestors.Add(type))
            {
                throw Error(aggregate, record, path, "cyclic record graph");
            }

            RawRecordNode element = Build(aggregate, record, type.GetGenericArguments()[0], path + "[]", ancestors, overrides, used);
            ancestors.Remove(type);
            return new RawRecordNode(type, CompileFactory(type), element);
        }

        if (type == typeof(object) || !type.IsClass || type.IsAbstract || type.IsInterface ||
            type.IsArray || typeof(IEnumerable).IsAssignableFrom(type) || typeof(Delegate).IsAssignableFrom(type) ||
            IsRuntimeType(type))
        {
            throw Error(aggregate, record, path, "forbidden raw type or unsupported collection/polymorphism");
        }

        // A concrete DTO is always decoded as its exact declared type. Discriminators/subtypes are not supported.
        if (!ancestors.Add(type))
        {
            throw Error(aggregate, record, path, "cyclic record graph");
        }

        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            throw Error(aggregate, record, path, "record requires an accessible parameterless constructor");
        }

        if (type.GetFields(BindingFlags.Instance | BindingFlags.Public).Length != 0)
        {
            throw Error(aggregate, record, path, "public fields are unsupported; use raw properties");
        }

        List<RawRecordProperty> properties = [];
        HashSet<string> storageNames = new(StringComparer.Ordinal);
        NullabilityInfoContext nullability = new();
        // DeclaredOnly traversal also detects hidden inherited properties instead of silently losing state.
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            foreach (PropertyInfo property in current.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                         .OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                string propertyPath = path + "." + property.Name;
                if (property.GetMethod?.IsPublic != true || property.SetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
                {
                    throw Error(aggregate, record, propertyPath, "inaccessible property binding");
                }

                overrides.TryGetValue((type, property.Name), out RecordPropertyPolicy? policy);
                if (policy is not null)
                {
                    used.Add((type, property.Name));
                }

                string name = policy?.StorageName ?? (property.Name == "Id" ? "_id" : property.Name);
                if (string.IsNullOrWhiteSpace(name) || name.Contains('.') || name.Contains('$') || name.Contains('\0') ||
                    name == "_t" || !storageNames.Add(name))
                {
                    throw Error(aggregate, record, propertyPath, "invalid or duplicate storage property name");
                }

                bool nullable = Nullable.GetUnderlyingType(property.PropertyType) is not null ||
                                (!property.PropertyType.IsValueType && nullability.Create(property).WriteState == NullabilityState.Nullable);
                bool allowMissing = policy?.AllowMissing ?? nullable;
                bool allowNull = policy?.AllowNull ?? nullable;
                if ((allowNull || allowMissing) && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) is null)
                {
                    throw Error(aggregate, record, propertyPath, "non-nullable scalar cannot allow missing/null");
                }

                RawRecordNode value = Build(aggregate, record, property.PropertyType, propertyPath, ancestors, overrides, used);
                properties.Add(new RawRecordProperty(property, name, allowMissing, allowNull, value));
            }
        }

        ancestors.Remove(type);
        if (properties.Count == 0)
        {
            throw Error(aggregate, record, path, "empty DTO or service is not a raw record");
        }

        return new RawRecordNode(type, CompileFactory(type), properties: properties.AsReadOnly());
    }

    private static bool IsScalar(Type type) =>
        type == typeof(string) || type == typeof(bool) || type == typeof(int) || type == typeof(long) ||
        type == typeof(double) || type == typeof(decimal) || type == typeof(Guid) ||
        type == typeof(DateTime) || type == typeof(DateOnly);

    private static bool IsRuntimeType(Type type)
    {
        for (Type? current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            string name = current.Namespace ?? "";
            if (current == typeof(Synonms.Structur.Domain.ValueObjects.UserAction) ||
                name.StartsWith("Synonms.Structur.Domain.Aggregates", StringComparison.Ordinal) ||
                name.StartsWith("Synonms.Structur.Core", StringComparison.Ordinal) ||
                name.StartsWith("Synonms.Structur.Api", StringComparison.Ordinal) ||
                name.StartsWith("MongoDB", StringComparison.Ordinal) ||
                (name == "System" || name.StartsWith("System.", StringComparison.Ordinal)))
            {
                return true;
            }
        }

        return type.GetInterfaces().Any(contract =>
            (contract.Namespace ?? "").StartsWith("Synonms.Structur.Domain", StringComparison.Ordinal));
    }

    private static Func<object> CompileFactory(Type type) =>
        Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(type), typeof(object))).Compile();

    private static InvalidOperationException Error(Type aggregate, Type record, string path, string reason) =>
        new($"Persistence mapping {aggregate.Name}/{record.Name} at {path}: {reason}.");
}

public sealed class RawRecordNode
{
    internal RawRecordNode(Type type, Func<object>? create = null, RawRecordNode? element = null, IReadOnlyList<RawRecordProperty>? properties = null)
    {
        Type = type;
        Create = create;
        Element = element;
        Properties = properties ?? [];
    }

    public Type Type { get; }
    public bool IsNullable => Element is not null && Create is null;
    public Func<object>? Create { get; }
    public RawRecordNode? Element { get; }
    public IReadOnlyList<RawRecordProperty> Properties { get; }
}

public sealed class RawRecordProperty
{
    internal RawRecordProperty(PropertyInfo property, string storageName, bool allowMissing, bool allowNull, RawRecordNode value)
    {
        Name = property.Name;
        StorageName = storageName;
        AllowMissing = allowMissing;
        AllowNull = allowNull;
        Value = value;
        ParameterExpression instance = Expression.Parameter(typeof(object));
        ParameterExpression input = Expression.Parameter(typeof(object));
        MemberExpression member = Expression.Property(Expression.Convert(instance, property.DeclaringType!), property);
        Get = Expression.Lambda<Func<object, object?>>(Expression.Convert(member, typeof(object)), instance).Compile();
        Set = Expression.Lambda<Action<object, object?>>(Expression.Assign(member, Expression.Convert(input, property.PropertyType)), instance, input).Compile();
    }

    public string Name { get; }
    public string StorageName { get; }
    public bool AllowMissing { get; }
    public bool AllowNull { get; }
    public RawRecordNode Value { get; }
    public Func<object, object?> Get { get; }
    public Action<object, object?> Set { get; }
}
