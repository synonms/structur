using Synonms.Structur.Application.Pipeline;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Queries;

public class ReadResourceCollectionQuery<TAggregateRoot, TResource> : Query
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public ReadResourceCollectionQuery(int limit)
    {
        Limit = limit;
    }

    public int Limit { get; }
    
    public int Offset { get; init; } = 0;

    public QueryParameters QueryParameters { get; init; } = new();
    
    public IEnumerable<SortItem> SortItems { get; init; } = new List<SortItem>();
}