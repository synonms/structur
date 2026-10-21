using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.System;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class ReadResourceCollectionQueryProcessor<TAggregateRoot, TResource> :
    IQueryHandler<ReadResourceCollectionQuery<TAggregateRoot, TResource>, ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IReadAggregateRepository<TAggregateRoot> _readAggregateRepository;
    private readonly IResourceMapper<TAggregateRoot, TResource> _resourceMapper;

    public ReadResourceCollectionQueryProcessor(IReadAggregateRepository<TAggregateRoot> readAggregateRepository,
        IResourceMapper<TAggregateRoot, TResource> resourceMapper)
    {
        _readAggregateRepository = readAggregateRepository;
        _resourceMapper = resourceMapper;
    }

    public async Task<Result<ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>>> HandleAsync(
        ReadResourceCollectionQuery<TAggregateRoot, TResource> query, CancellationToken cancellationToken)
    {
        if (query.Offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(query.Offset), "Collection offset must be nonnegative.");
        }

        AggregateQuery<TAggregateRoot> aggregateQuery = BuildQuery(query)
            .Skip(query.Offset)
            .Take(query.Limit);

        // Total and page are separate scoped reads, not a snapshot under concurrent writes.
        long total = await _readAggregateRepository.CountAsync(aggregateQuery, cancellationToken);
        IReadOnlyList<TAggregateRoot> aggregateRoots = await _readAggregateRepository.ListAsync(aggregateQuery, cancellationToken);

        List<ResourceEnvelope<TResource>> envelopes = aggregateRoots.Select(_resourceMapper.MapEnvelope).ToList();
        List<TResource> resources = envelopes.Select(envelope => envelope.Data).ToList();
        PaginatedList<TResource> paginatedResources = PaginatedList<TResource>.Create(
            resources, query.Offset, query.Limit, (int)Math.Min(total, int.MaxValue));

        return new ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>(paginatedResources, envelopes);
    }

    private static AggregateQuery<TAggregateRoot> BuildQuery(ReadResourceCollectionQuery<TAggregateRoot, TResource> query)
    {
        AggregateQuery<TAggregateRoot> aggregateQuery = AggregateQuery<TAggregateRoot>.All;
        IReadOnlyDictionary<string, object> parameters = query.QueryParameters.GetFiltersOnly();
        if (parameters.Count > 0)
        {
            ParameterExpression parameter = Expression.Parameter(typeof(TAggregateRoot), "aggregate");
            List<Expression> predicates = [];
            foreach ((string key, object expectedValue) in parameters)
            {
                PropertyInfo? aggregateProperty = typeof(TAggregateRoot).GetProperty(
                    key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (aggregateProperty is null)
                {
                    continue;
                }

                Expression field = Expression.Property(parameter, aggregateProperty);
                PropertyInfo? valueProperty = aggregateProperty.PropertyType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                if (valueProperty is not null)
                {
                    field = Expression.Property(field, valueProperty);
                }

                Type fieldType = Nullable.GetUnderlyingType(field.Type) ?? field.Type;
                Expression constant;
                if (expectedValue is null)
                {
                    constant = Expression.Constant(null, field.Type);
                }
                else
                {
                    object normalized = NormalizeParameter(expectedValue, fieldType, key);
                    constant = Expression.Constant(normalized, fieldType);
                    if (field.Type != fieldType)
                    {
                        constant = Expression.Convert(constant, field.Type);
                    }
                }

                predicates.Add(Expression.Equal(field, constant));
            }

            if (predicates.Count > 0)
            {
                Expression combined = predicates.Aggregate(Expression.AndAlso);
                aggregateQuery = aggregateQuery.Where(Expression.Lambda<Func<TAggregateRoot, bool>>(combined, parameter));
            }
        }

        ParameterExpression sortParameter = Expression.Parameter(typeof(TAggregateRoot), "aggregate");
        Expression<Func<TAggregateRoot, object>>? sort = null;
        foreach (SortItem item in query.SortItems)
        {
            PropertyInfo? property = typeof(TAggregateRoot).GetProperty(
                item.PropertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property is null)
            {
                throw new ArgumentException($"Unknown aggregate sort field '{item.PropertyName}'.", nameof(query.SortItems));
            }

            Expression field = Expression.Property(sortParameter, property);
            PropertyInfo? valueProperty = property.PropertyType.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
            if (valueProperty is not null)
            {
                field = Expression.Property(field, valueProperty);
            }

            sort = Expression.Lambda<Func<TAggregateRoot, object>>(Expression.Convert(field, typeof(object)), sortParameter);
            aggregateQuery = AddSort(aggregateQuery, sort, item.Direction == SortItem.SortDirection.Descending);
        }

        if (sort is null)
        {
            // Stable default page order, with the BSON identity field as the deterministic tie-breaker.
            Expression id = Expression.Property(Expression.Property(sortParameter, nameof(AggregateRoot<TAggregateRoot>.Id)), "Value");
            sort = Expression.Lambda<Func<TAggregateRoot, object>>(Expression.Convert(id, typeof(object)), sortParameter);
            aggregateQuery = AddSort(aggregateQuery, sort, descending: false);
        }

        return aggregateQuery;
    }

    private static AggregateQuery<TAggregateRoot> AddSort(AggregateQuery<TAggregateRoot> query,
        Expression<Func<TAggregateRoot, object>> key, bool descending) =>
        query.SortKeys.Count == 0
            ? query.OrderBy(key, descending)
            : query.ThenBy(key, descending);

    private static object NormalizeParameter(object value, Type destinationType, string propertyName)
    {
        if (destinationType.IsInstanceOfType(value))
        {
            return value;
        }

        try
        {
            if (destinationType == typeof(Guid))
            {
                return Guid.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!);
            }

            if (destinationType == typeof(DateOnly))
            {
                return DateOnly.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, destinationType, CultureInfo.InvariantCulture);
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException or ArgumentException)
        {
            throw new ArgumentException($"Query parameter for '{propertyName}' is incompatible with its declared field type.", nameof(value));
        }
    }
}
