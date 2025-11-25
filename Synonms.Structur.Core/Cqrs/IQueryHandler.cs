using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Core.Cqrs;

public interface IQueryHandler
{
}

public interface IQueryHandler<in TQuery, TQueryResponse> : IQueryHandler
    where TQuery : Query
    where TQueryResponse : QueryResponse
{
    public Task<Result<TQueryResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}