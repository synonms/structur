using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Testing.Tests;

public interface IDeleteTestFeature<TAggregateRoot> where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    string CollectionPath { get; }
    
    ArrangeAggregateInfo<TAggregateRoot> GenerateUniqueAggregate(EntityId<TAggregateRoot> id);

    Task<TAggregateRoot> PersistAggregateAsync(IServiceScopeFactory serviceScopeFactory, ArrangeAggregateInfo<TAggregateRoot> arrangeAggregateInfo);

    Task<TAggregateRoot?> RetrieveAggregateAsync(IServiceScopeFactory serviceScopeFactory, EntityId<TAggregateRoot> id);
}