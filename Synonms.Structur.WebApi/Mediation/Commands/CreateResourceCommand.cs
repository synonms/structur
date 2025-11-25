using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;

namespace Synonms.Structur.WebApi.Mediation.Commands;

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