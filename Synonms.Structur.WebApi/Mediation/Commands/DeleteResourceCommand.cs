using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class DeleteResourceCommand<TAggregateRoot> : Command
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public DeleteResourceCommand(EntityId<TAggregateRoot> id, Func<TAggregateRoot, bool>? filter = null)
    {
        Id = id;
        Filter = filter;
    }

    public EntityId<TAggregateRoot> Id { get; }
    
    public Func<TAggregateRoot, bool>? Filter { get; }
}