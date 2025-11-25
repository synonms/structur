using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class CreateResourceCommandResponse<TAggregateRoot> : CommandResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public CreateResourceCommandResponse(TAggregateRoot aggregateRoot)
    {
        AggregateRoot = aggregateRoot;
    }

    public TAggregateRoot AggregateRoot { get; }
}