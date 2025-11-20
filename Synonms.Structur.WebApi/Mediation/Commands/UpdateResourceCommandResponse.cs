using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class UpdateResourceCommandResponse<TAggregateRoot> : CommandResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public UpdateResourceCommandResponse(TAggregateRoot aggregateRoot)
    {
        AggregateRoot = aggregateRoot;
    }

    public TAggregateRoot AggregateRoot { get; }
}