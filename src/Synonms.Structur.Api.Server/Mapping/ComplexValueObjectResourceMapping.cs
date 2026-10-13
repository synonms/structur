using System.Collections;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Domain.System;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Api.Server.Mapping;

/// <summary>
/// Cached output-only mapping of complex data to resource members. Input resources continue to be
/// validated by aggregate factories. Register an adapter before use for a noncanonical resource shape.
/// </summary>
public static class ComplexValueObjectResourceMapping
{
    private static readonly ConcurrentDictionary<Type, Func<object, object>> Projections = new();
    private static readonly ConcurrentDictionary<(Type Source, Type Destination), Func<object, object>> Plans = new();
    private static readonly ConcurrentDictionary<(Type Source, Type Destination), Func<object, object>> Adapters = new();

    internal static bool SupportsSource(Type type) =>
        type.IsComplexValueObject()
        || (type.IsArray && type.GetElementType()?.IsComplexValueObject() == true)
        || type.GetInterfaces().Append(type).Any(contract =>
            contract.IsGenericType
            && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>)
            && contract.GetGenericArguments()[0].IsComplexValueObject());

    public static void Register<TSource, TDestination>(Func<TSource, TDestination> adapter)
        where TSource : class
        where TDestination : class
    {
        ArgumentNullException.ThrowIfNull(adapter);
        if (!Adapters.TryAdd((typeof(TSource), typeof(TDestination)), source => adapter((TSource)source)))
        {
            throw new InvalidOperationException(
                $"A complex value-object adapter is already registered for {typeof(TSource).FullName} -> {typeof(TDestination).FullName}.");
        }
    }

    public static object? Map(Type sourceType, Type destinationType, object? value)
    {
        if (value is null)
        {
            return null;
        }
        if (Adapters.TryGetValue((sourceType, destinationType), out Func<object, object>? adapter))
        {
            return adapter(value);
        }
        Type? contract = sourceType.GetComplexValueObjectContract();
        if (contract is not null)
        {
            value = Projections.GetOrAdd(sourceType, _ => CreateProjection(contract))(value);
            sourceType = contract.GetGenericArguments()[1];
        }
        return MapData(sourceType, destinationType, value);
    }

    private static Func<object, object> CreateProjection(Type contract)
    {
        MethodInfo method = typeof(ComplexValueObjectResourceMapping)
            .GetMethod(nameof(Project), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(contract.GetGenericArguments());
        return method.CreateDelegate<Func<object, object>>();
    }

    private static object Project<TSelf, TData>(object source)
        where TSelf : class, IComplexValueObject<TSelf, TData>
        where TData : class => ((TSelf)source).ToData();

    private static object? MapData(Type sourceType, Type destinationType, object? value)
    {
        if (value is null)
        {
            return null;
        }
        if (Adapters.TryGetValue((sourceType, destinationType), out Func<object, object>? adapter))
        {
            return adapter(value);
        }
        if (sourceType.IsComplexValueObject())
        {
            return Map(sourceType, destinationType, value);
        }
        destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        sourceType = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
        if (sourceType == typeof(string) && destinationType.IsEnum)
        {
            return string.IsNullOrWhiteSpace((string)value) ? null : Enum.Parse(destinationType, (string)value);
        }
        Type? sourceElement = GetEnumerableElementType(sourceType);
        Type? destinationElement = GetEnumerableElementType(destinationType);
        if (sourceElement is not null || destinationElement is not null)
        {
            if (sourceElement is null || destinationElement is null || value is not IEnumerable sequence)
            {
                throw new NotSupportedException($"Complex mapping from {sourceType.Name} to {destinationType.Name} requires a collection adapter.");
            }
            IList list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(destinationElement))!;
            foreach (object? item in sequence)
            {
                list.Add(MapData(sourceElement, destinationElement, item));
            }
            if (destinationType.IsArray)
            {
                Array array = Array.CreateInstance(destinationElement, list.Count);
                list.CopyTo(array, 0);
                return array;
            }
            if (!destinationType.IsAssignableFrom(list.GetType()))
            {
                throw new NotSupportedException($"Complex mapping collection {destinationType.Name} requires an adapter.");
            }
            return list;
        }
        if (destinationType.IsAssignableFrom(sourceType))
        {
            return value;
        }
        if (!sourceType.IsClass || !destinationType.IsClass || sourceType == typeof(string) || destinationType == typeof(string))
        {
            throw new NotSupportedException($"Complex mapping from {sourceType.Name} to {destinationType.Name} requires an adapter.");
        }
        return Plans.GetOrAdd((sourceType, destinationType), key => CreatePlan(key.Source, key.Destination))(value);
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        if (type == typeof(string))
        {
            return null;
        }
        if (type.IsArray)
        {
            if (type.GetArrayRank() != 1 || !type.IsSZArray)
            {
                throw new NotSupportedException($"Complex mapping collection {type.Name} requires an adapter for non-vector arrays.");
            }
            return type.GetElementType();
        }
        Type[] elementTypes = type.GetInterfaces().Append(type)
            .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(contract => contract.GetGenericArguments()[0])
            .Distinct()
            .ToArray();
        if (elementTypes.Length > 1 || (elementTypes.Length == 0 && typeof(IEnumerable).IsAssignableFrom(type)))
        {
            throw new NotSupportedException($"Complex mapping collection {type.Name} requires an adapter: expected exactly one IEnumerable<T> element type.");
        }
        return elementTypes.SingleOrDefault();
    }

    private static Func<object, object> CreatePlan(Type sourceType, Type destinationType)
    {
        ConstructorInfo constructor = destinationType.GetConstructor(Type.EmptyTypes)
            ?? throw new NotSupportedException($"Complex mapping destination {destinationType.Name} requires a public parameterless constructor or an adapter.");
        Func<object> create = Expression.Lambda<Func<object>>(
            Expression.Convert(Expression.New(constructor), typeof(object))).Compile();
        List<(Func<object, object?> Read, Action<object, object?> Write, Type Source, Type Destination)> bindings = [];
        foreach (PropertyInfo destination in destinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            PropertyInfo? source = sourceType.GetProperty(destination.Name, BindingFlags.Instance | BindingFlags.Public);
            if (destination.SetMethod?.IsPublic != true || source?.GetMethod?.IsPublic != true
                || destination.GetIndexParameters().Length != 0 || source.GetIndexParameters().Length != 0)
            {
                continue;
            }
            ParameterExpression instance = Expression.Parameter(typeof(object));
            ParameterExpression member = Expression.Parameter(typeof(object));
            Func<object, object?> read = Expression.Lambda<Func<object, object?>>(
                Expression.Convert(Expression.Property(Expression.Convert(instance, sourceType), source), typeof(object)), instance).Compile();
            Action<object, object?> write = Expression.Lambda<Action<object, object?>>(
                Expression.Assign(Expression.Property(Expression.Convert(instance, destinationType), destination),
                    Expression.Convert(member, destination.PropertyType)), instance, member).Compile();
            bindings.Add((read, write, source.PropertyType, destination.PropertyType));
        }
        return source =>
        {
            object destination = create();
            foreach ((Func<object, object?> read, Action<object, object?> write, Type from, Type to) in bindings)
            {
                write(destination, MapData(from, to, read(source)));
            }
            return destination;
        };
    }
}
