using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Commands;

public class CreateResourceCommand<TAggregateRoot, TResource> : Command
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    public CreateResourceCommand(TResource resource)
    {
        Resource = resource;
    }

    public TResource Resource { get; }
}