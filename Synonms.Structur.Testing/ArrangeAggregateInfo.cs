using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Testing;

public class ArrangeAggregateInfo<TAggregateRoot> 
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public ArrangeAggregateInfo(TAggregateRoot aggregateRoot, params object[]? prerequisiteEntities)
    {
        AggregateRoot = aggregateRoot;
        PrerequisiteEntities = new ArrangeEntitiesInfo(prerequisiteEntities);
    }

    public TAggregateRoot AggregateRoot { get; } 
        
    public ArrangeEntitiesInfo PrerequisiteEntities { get; }
} 