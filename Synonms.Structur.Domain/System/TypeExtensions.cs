using System.Reflection;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.System;

public static class TypeExtensions
{
    public static Type? GetSimpleValueObjectValueType(this Type type)
    {
        if (!type.IsSimpleValueObject())
        {
            return null;
        }

        PropertyInfo? valueObjectValuePropertyInfo = type.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

        if (valueObjectValuePropertyInfo is not null)
        {
            return valueObjectValuePropertyInfo.PropertyType;
        }

        Type? baseType = type.BaseType?.GetGenericTypeDefinition();
        
        if (baseType == typeof(DateOnlyValueObject)) return typeof(DateOnly);
        if (baseType == typeof(DateTimeValueObject)) return typeof(DateTime);
        if (baseType == typeof(DecimalValueObject)) return typeof(decimal);
        if (baseType == typeof(DoubleValueObject)) return typeof(double);
        if (baseType == typeof(IntValueObject)) return typeof(int);
        if (baseType == typeof(LongValueObject)) return typeof(long);
        if (baseType == typeof(StringValueObject)) return typeof(string);

        if (baseType == typeof(SimpleValueObject<>)) return baseType.GenericTypeArguments[0];

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

    public static bool IsSimpleValueObject(this Type type) => 
        type is
        {
            BaseType: not null
        }
        && (type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(SimpleValueObject<>)
            || type.BaseType.IsSimpleValueObject());
    
    public static bool IsComplexValueObject(this Type type) => 
        type is
        {
            IsInterface: false, 
            IsAbstract: false, 
            BaseType: not null
        } 
        && type.BaseType == typeof(ComplexValueObject);
}