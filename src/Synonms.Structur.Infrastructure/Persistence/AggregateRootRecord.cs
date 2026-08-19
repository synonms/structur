using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Infrastructure.Persistence;

public abstract class AggregateRootRecord<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public Guid Id { get; set; } = Guid.Empty;
    
    public Guid TenantId { get; set; } = Guid.Empty;

    public Guid EntityTag { get; set; } = Guid.Empty;

    public UserActionRecord CreatedAction { get; set; } = new();

    public UserActionRecord? UpdatedAction { get; set; }

    public UserActionRecord? DeletedAction { get; set; }
}