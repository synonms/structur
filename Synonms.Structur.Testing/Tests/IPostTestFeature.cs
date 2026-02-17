using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Testing.Tests;

public interface IPostTestFeature<TAggregateRoot, TResource> where TAggregateRoot : AggregateRoot<TAggregateRoot> where TResource : Resource
{
    string CollectionPath { get; }
    
    TResource GenerateInvalidResource(EntityId<TAggregateRoot> id);

    TResource GenerateValidResource(EntityId<TAggregateRoot> id);
    
    Task PersistPrerequisitesAsync(ArrangeEntitiesInfo arrangeEntitiesInfo);
    
    Task<TAggregateRoot?> RetrieveAggregateAsync(IServiceScopeFactory serviceScopeFactory, EntityId<TAggregateRoot> id);
    
    void ValidateCreatedAggregate(TAggregateRoot aggregateRoot, TResource resource);
}