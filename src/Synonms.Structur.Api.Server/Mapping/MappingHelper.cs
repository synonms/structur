using System.Collections;
using System.Reflection;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Api.Server.Routing;
using Synonms.Structur.Api.Server.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Lookups;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Api.Server.Mapping;

public static class MappingHelper
{
    internal static object? MapProperty<TDestination>(
        ResourceMappingBinding binding,
        object? sourceValue,
        ResourceMetadata metadata,
        IResourceMapperFactory resourceMapperFactory,
        IChildResourceMapperFactory childResourceMapperFactory,
        IRouteGenerator routeGenerator,
        string sourceEntityTypeName,
        Guid sourceEntityId)
    {
        if (binding.Override is not null)
        {
            return binding.Override(sourceValue);
        }

        PropertyInfo destination = binding.Destination;
        Type? sourceType = binding.SourceType;
        Type destinationType = destination.PropertyType;
        if (binding.IsComplexValueObject)
        {
            return ComplexValueObjectResourceMapping.Map(sourceType!, destinationType, sourceValue);
        }

        return binding.PropertyType switch
        {
            ResourcePropertyType.EmbeddedResource => IsAggregateMemberType(sourceType)
                ? MapEmbeddedChildResource(childResourceMapperFactory, sourceType, destinationType, sourceValue, metadata)
                : MapEmbeddedResource(resourceMapperFactory, sourceType, destinationType, sourceValue, metadata),
            ResourcePropertyType.EmbeddedResourceCollection => IsAggregateMemberCollection(sourceType)
                ? MapEmbeddedChildResourceCollection(childResourceMapperFactory, sourceType, destinationType, sourceValue, metadata)
                : MapEmbeddedResourceCollection(resourceMapperFactory, sourceType, destinationType, sourceValue, metadata),
            ResourcePropertyType.EmbeddedLookupResource => MapOptionalLookup(sourceValue as Lookup),
            ResourcePropertyType.RelatedResource => MapRelatedResource<TDestination>(routeGenerator, metadata.Links, destination, sourceType, sourceValue),
            ResourcePropertyType.RelatedResourceCollection => MapRelatedResourceCollection(routeGenerator, metadata.Links, destination, sourceType, sourceEntityTypeName, sourceEntityId, sourceValue),
            ResourcePropertyType.VanillaCollection => MapVanillaCollection(sourceType, destinationType, sourceValue),
            ResourcePropertyType.Enumeration => MapEnumeration(sourceType, destinationType, sourceValue),
            ResourcePropertyType.VanillaScalar => MapVanillaScalar(sourceType, sourceValue),
            _ => throw new NotSupportedException(
                $"Resource property type '{binding.PropertyType}' is not supported for {binding.SourceType?.FullName} -> {destinationType.FullName}.{destination.Name}.")
        };
    }

    private static bool IsAggregateMemberType(Type? type) =>
        type?.IsAggregateMember() == true;

    private static bool IsAggregateMemberCollection(Type? type) =>
        type?.GetArrayOrEnumerableElementType()?.IsAggregateMember() == true;

    private static object? MapEnumeration(Type? sourceType, Type destinationType, object? sourceValue)
    {
        object? value = MapVanillaScalar(sourceType, sourceValue);
        Type enumType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;
        if (value is null || enumType.IsInstanceOfType(value))
        {
            return value;
        }
        if (value is string enumName)
        {
            return string.IsNullOrWhiteSpace(enumName) ? null : Enum.Parse(enumType, enumName);
        }
        return value;
    }

    public static object? ConvertValue(Type fromType, Type toType, object? fromValue)
    {
        if (fromValue is null) return null;

        if (ComplexValueObjectResourceMapping.SupportsSource(fromType))
        {
            return ComplexValueObjectResourceMapping.Map(fromType, toType, fromValue);
        }

        if (fromType.IsValueObject())
        {
            object? rawValue = fromType.GetValueObjectValue(fromValue);

            // ValueObject<string> to Enum e.g. AddressType -> AddressTypeEnumeration
            if (fromType.GetValueObjectValueType() == typeof(string) && toType.IsEnum)
            {
                string? valueAsString = rawValue as string;
                object? convertedEnumValue = string.IsNullOrWhiteSpace(valueAsString) ? null : Enum.Parse(toType, valueAsString);
                return convertedEnumValue;
            }

            return rawValue;
        }

        return fromValue;
    }

    public static object? MapEmbeddedResource(IResourceMapperFactory resourceMapperFactory, Type? sourceAggregateRootType, Type destinationResourceType, object? aggregateRootValue, ResourceMetadata parentMetadata)
    {
        // TAggregateRoot.TAggregateRoot to TResource.TResource : A related aggregate where we present an embedded resource.

        if (sourceAggregateRootType is null) return null;

        IResourceMapper resourceMapper = resourceMapperFactory.Create(sourceAggregateRootType, destinationResourceType)
            ?? throw new InvalidOperationException($"No resource mapper is registered for embedded mapping {sourceAggregateRootType.FullName} -> {destinationResourceType.FullName}.");

        ResourceEnvelope<object>? envelope = aggregateRootValue is null ? null : resourceMapper.MapEnvelope(aggregateRootValue);
        object? destinationResourceValue = envelope?.Data;
        if (destinationResourceValue is not null && envelope is not null)
        {
            parentMetadata.AddEmbeddedMetadata(destinationResourceValue, envelope.Metadata);
        }

        return destinationResourceValue;
    }

    public static object? MapEmbeddedChildResource(IChildResourceMapperFactory childResourceMapperFactory, Type? sourceAggregateMemberType, Type destinationChildResourceType, object? aggregateMemberValue, ResourceMetadata parentMetadata)
    {
        // TAggregateRoot.TAggregateMember to TResource.TChildResource : A member where we present a nested child resource.

        if (sourceAggregateMemberType is null) return null;

        IChildResourceMapper childResourceMapper = childResourceMapperFactory.Create(sourceAggregateMemberType, destinationChildResourceType)
            ?? throw new InvalidOperationException($"No child resource mapper is registered for embedded mapping {sourceAggregateMemberType.FullName} -> {destinationChildResourceType.FullName}.");

        ResourceEnvelope<object>? envelope = aggregateMemberValue is null ? null : childResourceMapper.MapEnvelope(aggregateMemberValue);
        object? destinationChildResourceValue = envelope?.Data;
        if (destinationChildResourceValue is not null && envelope is not null)
        {
            parentMetadata.AddEmbeddedMetadata(destinationChildResourceValue, envelope.Metadata);
        }

        return destinationChildResourceValue;
    }

    public static object? MapEmbeddedResourceCollection(IResourceMapperFactory resourceMapperFactory, Type? sourceAggregateRootCollectionType, Type destinationResourceCollectionType, object? aggregateRootCollectionValue, ResourceMetadata parentMetadata)
    {
        // TAggregateRoot.IEnumerable<TAggregateRoot> to TResource.IEnumerable<TResource> : A related resource collection where we present an embedded array.

        if (sourceAggregateRootCollectionType is null) return null;

        Type? sourceAggregateRootType = sourceAggregateRootCollectionType.GetArrayOrEnumerableElementType();

        if (sourceAggregateRootType is null || !sourceAggregateRootType.IsAggregateRoot()) return null;

        Type? destinationResourceType = destinationResourceCollectionType.GetArrayOrEnumerableElementType();

        if (destinationResourceType is null || !destinationResourceType.IsResource()) return null;

        Type destinationListType = typeof(List<>).MakeGenericType(destinationResourceType);

        IList destinationList = (IList)Activator.CreateInstance(destinationListType)!;

        if (aggregateRootCollectionValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? embeddedResource = MapEmbeddedResource(resourceMapperFactory, sourceAggregateRootType, destinationResourceType, item, parentMetadata);

                if (embeddedResource is not null)
                {
                    destinationList?.Add(embeddedResource);
                }
            }
        }

        return destinationList;
    }

    public static object? MapEmbeddedChildResourceCollection(IChildResourceMapperFactory childResourceMapperFactory, Type? sourceAggregateMemberCollectionType, Type destinationChildResourceCollectionType, object? aggregateMemberCollectionValue, ResourceMetadata parentMetadata)
    {
        // TAggregateRoot.IEnumerable<TAggregateMember> to TResource.IEnumerable<TChildResource> : A member collection where we present a nested child resource array.

        if (sourceAggregateMemberCollectionType is null) return null;

        Type? sourceAggregateMemberType = sourceAggregateMemberCollectionType.GetArrayOrEnumerableElementType();

        if (sourceAggregateMemberType is null || !sourceAggregateMemberType.IsAggregateMember()) return null;

        Type? destinationChildResourceType = destinationChildResourceCollectionType.GetArrayOrEnumerableElementType();

        if (destinationChildResourceType is null || !destinationChildResourceType.IsResource()) return null;

        Type destinationListType = typeof(List<>).MakeGenericType(destinationChildResourceType);

        IList destinationList = (IList)Activator.CreateInstance(destinationListType)!;

        if (aggregateMemberCollectionValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                object? embeddedChildResource = MapEmbeddedChildResource(childResourceMapperFactory, sourceAggregateMemberType, destinationChildResourceType, item, parentMetadata);

                if (embeddedChildResource is not null)
                {
                    destinationList?.Add(embeddedChildResource);
                }
            }
        }

        return destinationList;
    }

    public static LookupResource MapLookup(Lookup lookup) =>
        new()
        {
            Id = lookup.Id.Value,
            LookupCode = lookup.LookupCode,
            LookupName = lookup.LookupName,
        };

    public static LookupResource? MapOptionalLookup(Lookup? lookup) =>
        lookup is null
            ? null
            : new LookupResource
            {
                Id = lookup.Id.Value,
                LookupCode = lookup.LookupCode,
                LookupName = lookup.LookupName,
            };

    public static object? MapRelatedResource<TDestination>(IRouteGenerator routeGenerator, ResourceLinks linksToAddTo, PropertyInfo resourcePropertyInfo, Type? entityIdType, object? aggregateRootValue)
    {
        // TAggregateRoot.EntityId<TEntity> to TResource.EntityId<TEntity>/Guid : A related resource where we pass the Id and potentially present a link.
        // or
        // TAggregateMember.EntityId<TEntity> to TChildResource.EntityId<TEntity>/Guid : A related resource where we pass the Id and potentially present a link.

        // Check if there is a related resource property, i.e. if this property is 'WidgetId' then look for 'Widget'
        string relatedResourcePropertyName = resourcePropertyInfo.Name.Replace("Id", string.Empty);

        if (typeof(TDestination).GetProperties(BindingFlags.Instance | BindingFlags.Public).Any(_ => _.Name.Equals(relatedResourcePropertyName)))
        {
            // Resource is embedded - don't add link
            return aggregateRootValue;
        }

        if (entityIdType is null || !entityIdType.IsEntityId()) return aggregateRootValue;
        if (aggregateRootValue is null) return null;

        Type relatedEntityType = entityIdType.GetGenericArguments().Single();

        Guid guid = Guid.Parse(aggregateRootValue.ToString()!);

        Uri relationUri = routeGenerator.Item(relatedEntityType, guid);
        Link relationLink = Link.RelationLink(relationUri);

        linksToAddTo.Add(relatedResourcePropertyName.ToCamelCase(), relationLink);

        return guid;
    }

    public static object? MapRelatedResourceCollection(IRouteGenerator routeGenerator, ResourceLinks linksToAddTo, PropertyInfo resourcePropertyInfo, Type? sourceCollectionType, string sourceEntityTypeName, Guid sourceEntityId, object? sourceValue)
    {
        // TAggregateRoot.IEnumerable<EntityId<TAggregateRoot>> to TResource.IEnumerable<EntityId<TAggregateRoot>/Guid>: A related resource collection where we present a link.
        // or
        // TAggregateRoot.IEnumerable<EntityId<TAggregateRoot>> to TResource.IEnumerable<EntityId<TAggregateRoot>/Guid>: A related resource collection where we present a link.
        // We only need the Id from the Aggregate for this (to build the url), not a related property value.

        Type? entityIdType = sourceCollectionType?.GetArrayOrEnumerableElementType();

        if (entityIdType is null || !entityIdType.IsEntityId())
        {
            throw new NotSupportedException(
                $"Related resource collection '{resourcePropertyInfo.Name}' requires a source collection of entity IDs.");
        }

        Type relatedEntityType = entityIdType.GetGenericArguments().Single();
        string parentIdPropertyName = sourceEntityTypeName.ToCamelCase() + "Id";

        // TODO: This likely won't work for AggregateMember and may not for Roots either - think of something else
        QueryParameters queryParameters = new()
        {
            [parentIdPropertyName] = sourceEntityId
        };
        Uri relationUri = routeGenerator.Collection(relatedEntityType, queryParameters);
        Link relationLink = Link.RelationLink(relationUri);

        linksToAddTo.Add(resourcePropertyInfo.Name.ToCamelCase(), relationLink);

        List<Guid> guids = [];

        if (sourceValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                if (Guid.TryParse(item.ToString(), out Guid guid))
                {
                    guids.Add(guid);
                }
            }
        }

        return guids;
    }

    public static object? MapVanillaScalar(Type? sourcePropertyType, object? sourceValue)
    {
        // Either a TAggregateRoot.SimpleValueObject -> TResource.VanillaScalar : A simple (single value) DDD value object property which we cast to a regular resource property
        // Or a EntityId<TEntity> -> TResource.Guid : Entity Id to Guid
        // Or a fallback to a vanilla -> vanilla

        if (sourcePropertyType is null) return null;

        if (sourcePropertyType.IsValueObject())
        {
            object? rawValue = sourcePropertyType.GetValueObjectValue(sourceValue);

            return rawValue;
        }

        if (sourcePropertyType.IsEntityId())
        {
            return sourcePropertyType.GetValueObjectValue(sourceValue);
        }

        return sourceValue;
    }

    public static object? MapVanillaCollection(Type? sourcePropertyCollectionType, Type destinationPropertyCollectionType, object? sourceValue)
    {
        if (sourcePropertyCollectionType is not null && ComplexValueObjectResourceMapping.SupportsSource(sourcePropertyCollectionType))
        {
            return ComplexValueObjectResourceMapping.Map(sourcePropertyCollectionType, destinationPropertyCollectionType, sourceValue);
        }

        // Either TAggregateRoot.IEnumerable<SimpleValueObject> -> TResource.IEnumerable<VanillaScalar> : A DDD ValueObject collection where we present a nested array of vanilla values.
        // Or fallback to IEnumerable<vanilla> -> IEnumerable<vanilla>

        Type? sourceVanillaType = sourcePropertyCollectionType?.GetArrayOrEnumerableElementType();

        if (sourceVanillaType is null) return null;

        Type? destinationVanillaType = destinationPropertyCollectionType.GetArrayOrEnumerableElementType();

        if (destinationVanillaType is null) return null;

        Type destinationListType = typeof(List<>).MakeGenericType(destinationVanillaType);

        IList destinationList = (IList)Activator.CreateInstance(destinationListType)!;

        if (sourceValue is IEnumerable enumerablePropertyValue)
        {
            foreach (object item in enumerablePropertyValue)
            {
                if (sourceVanillaType.IsValueObject())
                {
                    object? vanillaValue = MapVanillaScalar(sourceVanillaType, item);

                    if (vanillaValue is not null)
                    {
                        destinationList?.Add(vanillaValue);
                    }

                    continue;
                }

                destinationList?.Add(item);
            }
        }

        return destinationList;
    }
}
