using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.Projections;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.System;

public static class TypeExtensions
{
    public static Type? GetValueObjectValueType(this Type type)
    {
        if (!type.IsValueObject())
        {
            return null;
        }

        Type? valueObjectInterface = type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValueObject<,>));
        
        if (valueObjectInterface?.GenericTypeArguments.Length == 2)
        {
            return valueObjectInterface.GenericTypeArguments[1];
        }

        return null;
    }
    
    public static bool IsAggregateRoot(this Type type) =>
        type is
        {
            IsInterface: false, 
            IsAbstract: false, 
            BaseType.IsGenericType: true
        }
        && (type.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>) 
            || type.BaseType.BaseType is not null && type.BaseType.BaseType.IsGenericType && type.BaseType.BaseType.GetGenericTypeDefinition() == typeof(AggregateRoot<>));

    public static bool IsAggregateMember(this Type type) =>
        type is
        {
            IsInterface: false, 
            IsAbstract: false, 
            BaseType.IsGenericType: true
        }
        && (type.BaseType.GetGenericTypeDefinition() == typeof(AggregateMember<>)
            || type.BaseType.BaseType is not null && type.BaseType.BaseType.IsGenericType && type.BaseType.BaseType.GetGenericTypeDefinition() == typeof(AggregateMember<>));
    
    public static bool IsLookup(this Type type) =>
        !type.IsInterface
        && !type.IsAbstract
        && type.BaseType is not null
        && type.BaseType == typeof(Lookup);

    public static bool IsLookupId(this Type type) =>
        type == typeof(EntityId<Lookup>);

    public static bool IsValueObject(this Type type) =>
        type.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValueObject<,>));

    public static bool IsProjection(this Type type) =>
        type is
        {
            BaseType: not null
        }
        && (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(Projection<>)
            || type.BaseType.IsProjection());
}