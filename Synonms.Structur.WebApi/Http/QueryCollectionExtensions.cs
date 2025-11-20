using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Synonms.Structur.Application.Pipeline;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.WebApi.Http;

public static class QueryCollectionExtensions
{
    private const char DescendingPrefix = '-';
    
    public static int ExtractOffset(this IQueryCollection queryCollection) =>
        queryCollection.TryGetValue(QueryParameters.Names.Offset, out StringValues offsetValue)
            ? int.TryParse(offsetValue.FirstOrDefault(), out int parsedOffset) ? parsedOffset : 0
            : 0;
    
    public static QueryParameters ExtractQueryParameters<TAggregateRoot>(this IQueryCollection queryCollection) 
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
    {
        QueryParameters queryParameters = new();
        
        foreach ((string key, StringValues value) in queryCollection)
        {
            if (key.Equals(QueryParameters.Names.Offset, StringComparison.OrdinalIgnoreCase)
                || key.Equals(QueryParameters.Names.Sort, StringComparison.OrdinalIgnoreCase)) 
            {
                queryParameters.Add(QueryParameters.Names.Sort, value.ToString());   
                continue;
            }
            
            // TODO: Nested objects e.g. [child.property] = value

            PropertyInfo? aggregatePropertyInfo = typeof(TAggregateRoot).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            string valueAsString = value.ToString();
            
            if (aggregatePropertyInfo?.PropertyType.IsEntityId() ?? false)
            {
                if (Guid.TryParse(valueAsString, out Guid guid))
                {
                    ConstructorInfo? entityIdConstructor = aggregatePropertyInfo?.PropertyType.GetConstructor(new Type[] { typeof(Guid) });

                    if (entityIdConstructor is not null)
                    {
                        object entityId = entityIdConstructor.Invoke(new object?[] { guid });
                        queryParameters.Add(key, entityId);
                        continue;
                    }
                    
                    queryParameters.Add(key, guid);
                }
                
                continue;
            }

            if (aggregatePropertyInfo?.PropertyType.IsValueObject() ?? false)
            {
                object? valueForValueObject = GetValueForValueObject(aggregatePropertyInfo.PropertyType, valueAsString);

                if (valueForValueObject is not null)
                {
                    queryParameters.Add(key, valueForValueObject);
                }
                
                continue;
            }

            // TODO: Convert any other non-string/EntityId/ValueObject types

            queryParameters.Add(key, value.ToString());
        }

        return queryParameters;
    }

    public static IEnumerable<SortItem> ExtractSortItems(this IQueryCollection queryCollection)
    {
        if (queryCollection.TryGetValue(QueryParameters.Names.Sort, out StringValues sortValues) is false)
        {
            return Enumerable.Empty<SortItem>();
        }

        IEnumerable<SortItem> sortItems = sortValues
            .Where(sortValue => string.IsNullOrWhiteSpace(sortValue) is false)
            .Select(sortValue =>
            {
                bool isDescending = sortValue?.StartsWith(DescendingPrefix) ?? false;
                
                return new SortItem()
                {
                    Direction = isDescending 
                        ? SortItem.SortDirection.Descending 
                        : SortItem.SortDirection.Ascending,
                    PropertyName = sortValue?.TrimStart(DescendingPrefix).Trim().ToPascalCase() ?? string.Empty
                };
            });

        return sortItems;
    }
    
    private static object? GetValueForValueObject(Type valueObjectPropertyType, string value)
    {
        Type? valueObjectValueType = valueObjectPropertyType.GetValueObjectValueType();

        if (valueObjectValueType == typeof(string))
        {
            return value;
        }

        if (valueObjectValueType == typeof(short) && short.TryParse(value, out short valueAsShort))
        {
            return valueAsShort;
        }

        if (valueObjectValueType == typeof(int) && int.TryParse(value, out int valueAsInt))
        {
            return valueAsInt;
        }

        if (valueObjectValueType == typeof(long) && long.TryParse(value, out long valueAsLong))
        {
            return valueAsLong;
        }

        if (valueObjectValueType == typeof(double) && double.TryParse(value, out double valueAsDouble))
        {
            return valueAsDouble;
        }

        if (valueObjectValueType == typeof(float) && float.TryParse(value, out float valueAsFloat))
        {
            return valueAsFloat;
        }

        if (valueObjectValueType == typeof(decimal) && decimal.TryParse(value, out decimal valueAsDecimal))
        {
            return valueAsDecimal;
        }

        if (valueObjectValueType == typeof(bool) && bool.TryParse(value, out bool valueAsBool))
        {
            return valueAsBool;
        }

        if (valueObjectValueType == typeof(DateTime) && DateTime.TryParse(value, out DateTime valueAsDateTime))
        {
            return valueAsDateTime;
        }

        if (valueObjectValueType == typeof(DateOnly) && DateOnly.TryParse(value, out DateOnly valueAsDateOnly))
        {
            return valueAsDateOnly;
        }

        if (valueObjectValueType == typeof(TimeOnly) && TimeOnly.TryParse(value, out TimeOnly valueAsTimeOnly))
        {
            return valueAsTimeOnly;
        }

        return null;
    }
}