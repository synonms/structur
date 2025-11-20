using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Mapping;

public interface IAggregateUpdater<in TAggregateRoot, in TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    Task<Maybe<Fault>> UpdateAsync(TAggregateRoot aggregateRoot, TResource resource, CancellationToken cancellationToken);
}