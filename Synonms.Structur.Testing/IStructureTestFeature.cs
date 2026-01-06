using System.Text.Json;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Testing;

public interface IStructureTestFeature<TAggregateRoot, TResource> where TAggregateRoot : AggregateRoot<TAggregateRoot> where TResource : Resource
{
    string CollectionPath { get; }

    JsonSerializerOptions? JsonSerializerOptions { get; }

    TAggregateRoot GenerateUniqueAggregate(Action<Faker<TResource>>? customisationAction = null);
    
    Task<TAggregateRoot> PersistAggregateAsync(IServiceScopeFactory serviceScopeFactory, ArrangeAggregateInfo<TAggregateRoot> arrangeAggregateInfo);

    void ValidateResource(TAggregateRoot aggregateRoot, TResource resource);
}