using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Core.Mediation;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand, TCommandResponse> : ICommandHandler
    where TCommand : Command
    where TCommandResponse : CommandResponse
{
    public Task<Result<TCommandResponse>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}