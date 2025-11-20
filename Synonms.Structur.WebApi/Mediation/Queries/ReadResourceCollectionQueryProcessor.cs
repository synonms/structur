using System.Linq.Expressions;
using System.Reflection;
using Synonms.Structur.Application.Mapping;
using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.System;
using Synonms.Structur.WebApi.Linq;

namespace Synonms.Structur.WebApi.Mediation.Queries;

public class ReadResourceCollectionQueryProcessor<TAggregateRoot, TResource> : IQueryHandler<ReadResourceCollectionQuery<TAggregateRoot, TResource>, ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IAggregateRepository<TAggregateRoot> _aggregateRepository;
    private readonly IResourceMapper<TAggregateRoot, TResource> _resourceMapper;

    public ReadResourceCollectionQueryProcessor(IAggregateRepository<TAggregateRoot> aggregateRepository, IResourceMapper<TAggregateRoot, TResource> resourceMapper)
    {
        _aggregateRepository = aggregateRepository;
        _resourceMapper = resourceMapper;
    }

    public async Task<Result<ReadResourceCollectionQueryResponse<TAggregateRoot, TResource>>> HandleAsync(ReadResourceCollectionQuery<TAggregateRoot, TResource> query, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<string, object> filterParameters = query.QueryParameters.GetFiltersOnly();
        
        PaginatedList<TAggregateRoot> paginatedAggregateRoots = filterParameters.Any()
            ? PaginatedList<TAggregateRoot>.Create(_aggregateRepository.Query(ParametersPredicate(filterParameters)).ApplySort(query.SortItems), query.Offset, query.Limit)
            : await _aggregateRepository.ReadAllAsync(query.Offset, query.Limit, q => q.ApplySort(query.SortItems), cancellationToken);
        
        List<TResource> resources = paginatedAggregateRoots.Select(x => _resourceMapper.Map(x)).ToList();
        PaginatedList<TResource> paginatedResources = PaginatedList<TResource>.Create(resources, query.Offset, query.Limit, paginatedAggregateRoots.Size);

        ReadResourceCollectionQueryResponse<TAggregateRoot, TResource> response = new(paginatedResources);

        return response;
    }
    
    private static Expression<Func<TAggregateRoot, bool>> ParametersPredicate(IReadOnlyDictionary<string, object>? parameters)
    {
        if ((parameters?.Any() ?? false) is false)
        {
            return _ => true;
        }

        // (TAggregateRoot x)
        ParameterExpression xParameter = Expression.Parameter(typeof(TAggregateRoot), "x");

        List<Expression> columnExpressions = new();

        foreach ((string key, object expectedValue) in parameters)
        {
            // TODO: Nested objects e.g. [child.property] = value

            PropertyInfo? aggregatePropertyInfo = typeof(TAggregateRoot).GetProperty(key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (aggregatePropertyInfo is null)
            {
                continue;
            }
            
            // x.{columnName}
            MemberExpression columnNameExpression = Expression.Property(xParameter, aggregatePropertyInfo.Name);

            if (aggregatePropertyInfo.PropertyType.IsValueObject())
            {
                // x.{columnName}.Value
                columnNameExpression = Expression.Property(columnNameExpression, "Value");
            }

            // {searchValue}
            ConstantExpression expectedValueExpression = Expression.Constant(expectedValue);
            
            // TODO: Anything other than string likely breaks
            // Need to convert the incoming value (likely a string) to the same type as the underlying ValueType value

            // x.{columnName} == {searchValue}
            BinaryExpression equalExpression = Expression.Equal(columnNameExpression, expectedValueExpression);

            columnExpressions.Add(equalExpression);
        }

        if (columnExpressions.Count == 1)
        {
            Expression<Func<TAggregateRoot, bool>> lambdaExpression = Expression.Lambda<Func<TAggregateRoot, bool>>(columnExpressions[0], new ParameterExpression[] { xParameter });

            return lambdaExpression;
        }
        else
        {
            Expression combinedExpression = columnExpressions[0];

            for (int i = 1; i < columnExpressions.Count; i++)
            {
                combinedExpression = Expression.And(combinedExpression, columnExpressions[i]);
            }

            Expression<Func<TAggregateRoot, bool>> lambdaExpression = Expression.Lambda<Func<TAggregateRoot, bool>>(combinedExpression, new ParameterExpression[] { xParameter });

            return lambdaExpression;
        }
    }
}