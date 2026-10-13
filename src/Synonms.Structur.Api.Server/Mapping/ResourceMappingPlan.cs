using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Api.Server.Mapping;

/// <summary>Registers output-only conversions for a specific source/destination property pair.</summary>
public static class ResourceMappingConfiguration
{
    private static readonly ConcurrentDictionary<(Type Source, Type Destination, string Property), PropertyOverride> Overrides = new();

    public static void Register<TSource, TDestination>(
        string destinationPropertyName,
        string sourcePropertyName,
        Func<object?, object?> converter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationPropertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePropertyName);
        ArgumentNullException.ThrowIfNull(converter);

        ResourceMappingPlan.ValidateOverride(
            typeof(TSource),
            typeof(TDestination),
            destinationPropertyName,
            sourcePropertyName);

        (Type, Type, string) key = (typeof(TSource), typeof(TDestination), destinationPropertyName);
        if (!Overrides.TryAdd(key, new PropertyOverride(sourcePropertyName, converter)))
        {
            throw new InvalidOperationException(
                $"A resource mapping override is already registered for {typeof(TSource).FullName} -> {typeof(TDestination).FullName}.{destinationPropertyName}.");
        }

        ResourceMappingPlan.Invalidate(typeof(TSource), typeof(TDestination));
    }

    internal static bool TryGet(Type source, Type destination, string property, out PropertyOverride mapping) =>
        Overrides.TryGetValue((source, destination, property), out mapping!);
}

internal sealed record PropertyOverride(string SourcePropertyName, Func<object?, object?> Convert);

internal sealed record ResourceMappingBinding(
    PropertyInfo Destination,
    Func<object, object?>? ReadSource,
    Action<object, object?> WriteDestination,
    Type? SourceType,
    Func<object?, object?>? Override,
    ResourcePropertyType PropertyType,
    bool IsComplexValueObject);

/// <summary>Immutable cached property metadata shared by aggregate and member mappers.</summary>
internal sealed class ResourceMappingPlan
{
    private static readonly ConcurrentDictionary<(Type Source, Type Destination, long ConfigurationVersion), Lazy<ResourceMappingPlan>> Plans = new();
    private static long _configurationVersion;

    private ResourceMappingPlan(IReadOnlyList<ResourceMappingBinding> bindings) => Bindings = bindings;

    internal IReadOnlyList<ResourceMappingBinding> Bindings { get; }

    internal static ResourceMappingPlan Get(Type source, Type destination)
    {
        while (true)
        {
            long configurationVersion = Volatile.Read(ref _configurationVersion);
            Lazy<ResourceMappingPlan> lazyPlan = Plans.GetOrAdd(
                (source, destination, configurationVersion),
                key => new Lazy<ResourceMappingPlan>(
                    () => Build(key.Source, key.Destination),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            ResourceMappingPlan plan = lazyPlan.Value;
            if (configurationVersion == Volatile.Read(ref _configurationVersion))
            {
                return plan;
            }
        }
    }

    internal static void Invalidate(Type source, Type destination)
    {
        long configurationVersion = Interlocked.Increment(ref _configurationVersion);
        foreach ((Type cachedSource, Type cachedDestination, long cachedVersion) in Plans.Keys)
        {
            if (cachedSource == source && cachedDestination == destination && cachedVersion != configurationVersion)
            {
                Plans.TryRemove((cachedSource, cachedDestination, cachedVersion), out _);
            }
        }
    }

    internal static void ValidateOverride(Type sourceType, Type destinationType, string destinationName, string sourceName)
    {
        PropertyInfo[] destinations = GetDestinationProperties(destinationType)
            .Where(property => property.Name == destinationName)
            .ToArray();
        if (destinations.Length != 1 || destinations[0].SetMethod?.IsPublic != true)
        {
            throw MappingError(sourceType, destinationType, destinationName, "override destination property does not exist uniquely or is not publicly writable");
        }

        PropertyInfo[] sources = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.Name == sourceName
                && property.GetMethod?.IsPublic == true
                && property.GetIndexParameters().Length == 0)
            .ToArray();
        if (sources.Length != 1)
        {
            throw MappingError(sourceType, destinationType, destinationName, $"override source property '{sourceName}' does not exist uniquely or is not publicly readable");
        }
    }

    private static ResourceMappingPlan Build(Type sourceType, Type destinationType)
    {
        PropertyInfo[] sourceProperties = sourceType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0)
            .ToArray();
        PropertyInfo[] destinationProperties = GetDestinationProperties(destinationType).ToArray();
        List<ResourceMappingBinding> bindings = [];

        foreach (PropertyInfo destination in destinationProperties)
        {
            bool hasOverride = ResourceMappingConfiguration.TryGet(sourceType, destinationType, destination.Name, out PropertyOverride mapping);
            string sourceName = hasOverride ? mapping.SourcePropertyName : destination.Name;
            PropertyInfo[] matchingSources = sourceProperties.Where(property => property.Name == sourceName).ToArray();
            if (matchingSources.Length > 1)
            {
                throw MappingError(sourceType, destinationType, destination.Name, $"source property name '{sourceName}' is ambiguous");
            }

            PropertyInfo? source = matchingSources.SingleOrDefault();
            if (hasOverride && source is null)
            {
                throw MappingError(sourceType, destinationType, destination.Name, $"override source property '{sourceName}' does not exist or is not publicly readable");
            }

            if (destination.SetMethod?.IsPublic != true)
            {
                continue;
            }

            if (source is null)
            {
                // Resource-only fields retain their constructor/configured defaults.
                continue;
            }

            ValidateCompatibility(sourceType, destinationType, source, destination, hasOverride);
            bindings.Add(new ResourceMappingBinding(
                destination,
                CreateReader(sourceType, source),
                CreateWriter(destinationType, destination),
                source.PropertyType,
                hasOverride ? mapping.Convert : null,
                GetMappingPropertyType(source.PropertyType, destination.PropertyType),
                ComplexValueObjectResourceMapping.SupportsSource(source.PropertyType)));
        }

        return new ResourceMappingPlan(bindings.AsReadOnly());
    }

    private static IEnumerable<PropertyInfo> GetDestinationProperties(Type destinationType)
        => destinationType.GetResourceProperties();

    private static ResourcePropertyType GetMappingPropertyType(Type sourceType, Type destinationType)
    {
        Type? sourceElement = sourceType.GetArrayOrEnumerableElementType();
        Type? destinationElement = destinationType.GetArrayOrEnumerableElementType();
        Type sourceShape = sourceElement ?? sourceType;
        Type destinationShape = destinationElement ?? destinationType;

        if (sourceShape.IsAggregateMember() && destinationShape.IsClass)
        {
            return sourceElement is null ? ResourcePropertyType.EmbeddedResource : ResourcePropertyType.EmbeddedResourceCollection;
        }

        if (sourceShape.IsAggregateRoot() && destinationShape.IsClass)
        {
            return sourceElement is null ? ResourcePropertyType.EmbeddedResource : ResourcePropertyType.EmbeddedResourceCollection;
        }

        return destinationType.GetResourcePropertyType();
    }

    private static Func<object, object?> CreateReader(Type sourceType, PropertyInfo property)
    {
        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        return Expression.Lambda<Func<object, object?>>(
            Expression.Convert(Expression.Property(Expression.Convert(instance, sourceType), property), typeof(object)),
            instance).Compile();
    }

    private static Action<object, object?> CreateWriter(Type destinationType, PropertyInfo property)
    {
        ParameterExpression instance = Expression.Parameter(typeof(object), "instance");
        ParameterExpression value = Expression.Parameter(typeof(object), "value");
        return Expression.Lambda<Action<object, object?>>(
            Expression.Assign(
                Expression.Property(Expression.Convert(instance, destinationType), property),
                Expression.Convert(value, property.PropertyType)),
            instance,
            value).Compile();
    }

    private static void ValidateCompatibility(Type sourceType, Type destinationType, PropertyInfo source, PropertyInfo destination, bool overridden)
    {
        if (overridden || ComplexValueObjectResourceMapping.SupportsSource(source.PropertyType))
        {
            return;
        }

        ResourcePropertyType category = GetMappingPropertyType(source.PropertyType, destination.PropertyType);
        bool sourceIsCollection = source.PropertyType.GetArrayOrEnumerableElementType() is not null;
        bool destinationIsCollection = destination.PropertyType.GetArrayOrEnumerableElementType() is not null;
        if (category is ResourcePropertyType.VanillaCollection
            or ResourcePropertyType.EmbeddedResourceCollection
            or ResourcePropertyType.RelatedResourceCollection)
        {
            if (!sourceIsCollection || !destinationIsCollection)
            {
                throw MappingError(sourceType, destinationType, destination.Name,
                    $"collection source and destination shapes are incompatible ('{source.PropertyType.FullName}' -> '{destination.PropertyType.FullName}')");
            }
        }
        else if (sourceIsCollection || destinationIsCollection)
        {
            throw MappingError(sourceType, destinationType, destination.Name,
                $"scalar source and destination shapes are incompatible ('{source.PropertyType.FullName}' -> '{destination.PropertyType.FullName}')");
        }

        Type? sourceElement = source.PropertyType.GetArrayOrEnumerableElementType();
        Type? destinationElement = destination.PropertyType.GetArrayOrEnumerableElementType();
        Type from = sourceElement ?? source.PropertyType;
        Type to = Nullable.GetUnderlyingType(destinationElement ?? destination.PropertyType)
            ?? destinationElement
            ?? destination.PropertyType;
        bool compatible = category switch
        {
            ResourcePropertyType.VanillaScalar or ResourcePropertyType.VanillaCollection or ResourcePropertyType.Enumeration =>
                AreScalarTypesCompatible(from, to),
            ResourcePropertyType.RelatedResource =>
                (GetMappedScalarType(from) == typeof(Guid) && (from.IsEntityId() || from == typeof(Guid))) && to == typeof(Guid),
            ResourcePropertyType.RelatedResourceCollection =>
                from.IsEntityId() && to == typeof(Guid),
            ResourcePropertyType.EmbeddedResource =>
                (from.IsAggregateRoot() || from.IsAggregateMember()) && to.IsClass,
            ResourcePropertyType.EmbeddedResourceCollection =>
                (from.IsAggregateRoot() || from.IsAggregateMember()) && to.IsClass,
            ResourcePropertyType.EmbeddedLookupResource =>
                typeof(Lookup).IsAssignableFrom(from) && to == typeof(LookupResource),
            _ => false
        };
        if (!compatible)
        {
            throw MappingError(sourceType, destinationType, destination.Name,
                $"matched source type '{source.PropertyType.FullName}' cannot be mapped to destination type '{destination.PropertyType.FullName}'");
        }
    }

    private static bool AreScalarTypesCompatible(Type source, Type destination)
    {
        Type sourceType = GetMappedScalarType(source);
        Type destinationType = Nullable.GetUnderlyingType(destination) ?? destination;
        return destinationType.IsAssignableFrom(sourceType)
            || (sourceType == typeof(string) && destinationType.IsEnum);
    }

    private static Type GetMappedScalarType(Type type)
    {
        Type sourceType = Nullable.GetUnderlyingType(type) ?? type;
        if (sourceType.IsValueObject())
        {
            Type? valueType = sourceType.GetValueObjectValueType();
            if (valueType is not null)
            {
                sourceType = Nullable.GetUnderlyingType(valueType) ?? valueType;
            }
        }
        return sourceType.IsEntityId() ? typeof(Guid) : sourceType;
    }

    private static InvalidOperationException MappingError(Type source, Type destination, string property, string detail) =>
        new($"Cannot build resource mapping plan for {source.FullName} -> {destination.FullName}, property '{property}': {detail}.");
}
