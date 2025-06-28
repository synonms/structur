using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Core.Mediation;

public interface ICommandHandler
{
}

public interface ICommandHandler<in TCommand> : ICommandHandler
    where TCommand : Command
{
    public Task<Maybe<Fault>> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}