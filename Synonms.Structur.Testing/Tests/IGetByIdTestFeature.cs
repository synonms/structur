using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Testing.Tests;

public interface IGetByIdTestFeature<TAggregateRoot, TResource> where TAggregateRoot : AggregateRoot<TAggregateRoot> where TResource : Resource
{
    string CollectionPath { get; }

    ArrangeAggregateInfo<TAggregateRoot> GenerateUniqueAggregate(EntityId<TAggregateRoot> id);
    
    Task<TAggregateRoot> PersistAggregateAsync(IServiceScopeFactory serviceScopeFactory, ArrangeAggregateInfo<TAggregateRoot> arrangeAggregateInfo);

    void ValidateResource(TAggregateRoot aggregateRoot, TResource resource);
}