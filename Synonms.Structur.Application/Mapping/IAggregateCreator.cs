using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Mapping;

public interface IAggregateCreator<TAggregateRoot, in TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    Task<Result<TAggregateRoot>> CreateAsync(TResource resource, CancellationToken cancellationToken);
}