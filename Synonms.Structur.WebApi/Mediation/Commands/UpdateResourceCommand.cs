using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class UpdateResourceCommand<TAggregateRoot, TResource> : Command
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public UpdateResourceCommand(EntityId<TAggregateRoot> id, TResource resource)
    {
        Id = id;
        Resource = resource;
    }

    public EntityId<TAggregateRoot> Id { get; }
    
    public TResource Resource { get; }
    
    public EntityTag? IfMatch { get; init; }
}