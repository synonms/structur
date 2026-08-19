using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

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