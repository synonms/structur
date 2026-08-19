using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Commands;

public class CreateResourceCommandResponse<TAggregateRoot> : CommandResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public CreateResourceCommandResponse(TAggregateRoot aggregateRoot)
    {
        AggregateRoot = aggregateRoot;
    }

    public TAggregateRoot AggregateRoot { get; }
}