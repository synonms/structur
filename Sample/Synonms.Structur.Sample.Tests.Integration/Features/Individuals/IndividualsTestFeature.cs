using System.Text.Json;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Testing;
using Synonms.Structur.WebApi.Hypermedia.Default;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Individuals;

public class IndividualsTestFeature : IStructureTestFeature<Individual, IndividualResource>
{
    public string CollectionPath => "individuals";

    public JsonSerializerOptions? JsonSerializerOptions => DefaultOutputFormatter.JsonSerializerOptions;

    public Individual GenerateUniqueAggregate(Action<Faker<IndividualResource>>? customisationAction = null)
    {
        Faker<IndividualResource> fakerResource = new Faker<IndividualResource>()
            .CustomInstantiator(faker => new IndividualResource(Guid.NewGuid(), Link.EmptyLink()))
            .RuleFor(x => x.TenantReference, faker => faker.Random.AlphaNumeric(10))
            .RuleFor(x => x.Forename, faker => faker.Name.FirstName())
            .RuleFor(x => x.Surname, faker => faker.Name.LastName())
            .RuleFor(x => x.EmailAddresses, (faker, individualResource) => [
                new EmailAddressResource
                {
                    Address = faker.Internet.Email(individualResource.Forename, individualResource.Surname),
                    IsPrimary = true
                }
            ])
            .RuleFor(x => x.TelephoneNumbers, faker => []);
        
        customisationAction?.Invoke(fakerResource);
        
        IndividualResource resource =  fakerResource.Generate();
        
        IndividualCreatedEvent createdEvent = new((EntityId<Individual>)resource.Id, resource, TestTenant.Id);
        
        Result<Individual> createdResult = createdEvent.ApplyAsync(null).Result;
            
        return createdResult.Match(
            createdIndividual => createdIndividual,
            errors => throw new ApplicationException($"Unable to create Individual Id '{createdEvent.AggregateId}': {errors}"));
    }
    
    public async Task<Individual> PersistAggregateAsync(IServiceScopeFactory serviceScopeFactory, ArrangeAggregateInfo<Individual> arrangeAggregateInfo)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        IMongoCollection<Individual> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Individual>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Individual)]);

        await collection.InsertOneAsync(arrangeAggregateInfo.AggregateRoot, cancellationToken: TestContext.Current.CancellationToken);

        return arrangeAggregateInfo.AggregateRoot;
    }

    public void ValidateResource(Individual aggregateRoot, IndividualResource resource)
    {
        Assert.Equal(aggregateRoot.Id.Value, resource.Id);
        Assert.Equal(aggregateRoot.TenantReference, resource.TenantReference);
        Assert.Equal(aggregateRoot.FriendlyId, resource.FriendlyId);
        Assert.Equal(aggregateRoot.Salutation?.Value, resource.Salutation);
        Assert.Equal(aggregateRoot.Forename, resource.Forename);
        Assert.Equal(aggregateRoot.Surname, resource.Surname);
        foreach (EmailAddress emailAddress in aggregateRoot.EmailAddresses)
        {
            Assert.Contains(resource.EmailAddresses, x => x.Address == emailAddress.Address);
        }
        foreach (TelephoneNumber telephoneNumber in aggregateRoot.TelephoneNumbers)
        {
            Assert.Contains(resource.TelephoneNumbers, x => x.Number == telephoneNumber.Number);
        }
    }
}