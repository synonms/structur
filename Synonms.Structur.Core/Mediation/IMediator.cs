using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Core.Mediation;

public interface IMediator
{
    Task<Maybe<Fault>> SendCommandAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : Command;

    Task<Result<QueryResponse>> SendQueryAsync<TQuery, TQueryResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : Query
        where TQueryResponse : QueryResponse;
}