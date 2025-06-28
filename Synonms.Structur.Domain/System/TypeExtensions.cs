using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Domain.System;

public static class TypeExtensions
{
    public static Type? GetValueObjectValueType(this Type type) =>
        type.IsValueObject() 
            ? type.BaseType?.GetGenericArguments().FirstOrDefault()
            : null;
    
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
    
    // public static bool IsLookup(this Type type) =>
    //     !type.IsInterface
    //     && !type.IsAbstract
    //     && type.BaseType is not null
    //     && type.BaseType == typeof(Lookup);

    // public static bool IsLookupId(this Type type) =>
    //     type == typeof(EntityId<Lookup>);

    public static bool IsValueObject(this Type type) =>
        type is
        {
            IsInterface: false, 
            IsAbstract: false, 
            BaseType.IsGenericType: true
        }
        && type.BaseType.GetGenericTypeDefinition() == typeof(ValueObject<>);

    public static bool IsEntityId(this Type type) =>
        type is
        {
            IsInterface: false, 
            IsAbstract: false, 
            IsGenericType: true
        }
        && type.GetGenericTypeDefinition() == typeof(EntityId<>);
}