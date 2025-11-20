using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Core.Mediation;

public interface IMediator
{
    Task<Result<TCommandResponse>> SendCommandAsync<TCommand, TCommandResponse>(TCommand command, CancellationToken cancellationToken = default)
        where TCommand : Command
        where TCommandResponse : CommandResponse;

    Task<Result<TQueryResponse>> SendQueryAsync<TQuery, TQueryResponse>(TQuery query, CancellationToken cancellationToken = default)
        where TQuery : Query
        where TQueryResponse : QueryResponse;
}