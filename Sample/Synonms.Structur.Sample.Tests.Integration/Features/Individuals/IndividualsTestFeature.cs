using Bogus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Forms;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Testing;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Individuals;

public class IndividualsTestFeature : 
    IGetAllTestFeature<Individual, IndividualResource>, 
    IGetByIdTestFeature<Individual, IndividualResource>,
    IPostTestFeature<Individual, IndividualResource>,
    IPutTestFeature<Individual, IndividualResource>,
    IDeleteTestFeature<Individual>,
    ICreateFormTestFeature,
    IEditFormTestFeature<Individual>
{
    public string CollectionPath => "individuals";

    public IndividualResource GenerateInvalidResource(EntityId<Individual> id) =>
        new Faker<IndividualResource>()
            .CustomInstantiator(faker => new IndividualResource(id.Value, Link.EmptyLink()))
            .RuleFor(x => x.TenantReference, string.Empty)
            .RuleFor(x => x.Forename, string.Empty)
            .RuleFor(x => x.Surname, string.Empty)
            .RuleFor(x => x.EmailAddresses, [])
            .RuleFor(x => x.TelephoneNumbers, []);

    public IndividualResource GenerateValidResource(EntityId<Individual> id) =>
        new Faker<IndividualResource>()
            .CustomInstantiator(faker => new IndividualResource(id.Value, Link.EmptyLink()))
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
            .RuleFor(x => x.TelephoneNumbers, []);

    public ArrangeAggregateInfo<Individual> GenerateUniqueAggregate(EntityId<Individual> id)
    {
        IndividualResource resource = GenerateValidResource(id);
        IndividualCreatedEvent createdEvent = new(id, resource, TestTenant.Id);
        Result<Individual> createdResult = createdEvent.ApplyAsync(null).Result;
            
        return createdResult.Match(
            createdIndividual => new ArrangeAggregateInfo<Individual>(createdIndividual),
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
    
    public Task PersistPrerequisitesAsync(ArrangeEntitiesInfo arrangeEntitiesInfo) => 
        Task.CompletedTask;

    public async Task<Individual?> RetrieveAggregateAsync(IServiceScopeFactory serviceScopeFactory, EntityId<Individual> id)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        IMongoCollection<Individual> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Individual>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Individual)]);

        Individual? individual = (await collection.FindAsync(x => x.Id == id)).FirstOrDefault();

        return individual;
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

    public void ValidateCreatedAggregate(Individual aggregateRoot, IndividualResource resource)
    {
        ValidateCommonAggregateProperties(aggregateRoot, resource);

        Assert.Equal(resource.Id, aggregateRoot.Id.Value);
        Assert.Equal(resource.TenantReference, aggregateRoot.TenantReference);
        Assert.NotNull(aggregateRoot.FriendlyId);
    }

    public void ValidateUpdatedAggregate(Individual aggregateRoot, IndividualResource resource)
    {
        ValidateCommonAggregateProperties(aggregateRoot, resource);
    }

    public void ValidateCreateForm(Form form)
    {
        FormField? forenameField = form.Fields.SingleOrDefault(formField =>
            formField.Name.Equals(nameof(IndividualResource.Forename).ToCamelCase()));
        
        Assert.NotNull(forenameField);
        Assert.True(forenameField.IsRequired);
        Assert.Equal(Individual.ForenameMaxLength, forenameField.MaxLength);
        
        // TODO: Remaining fields;
    }

    public void ValidateEditForm(Form form, Individual aggregateRoot)
    {
        FormField? forenameField = form.Fields.SingleOrDefault(formField =>
            formField.Name.Equals(nameof(IndividualResource.Forename).ToCamelCase()));
        
        Assert.NotNull(forenameField);
        Assert.True(forenameField.IsRequired);
        Assert.Equal(Individual.ForenameMaxLength, forenameField.MaxLength);
        Assert.Equal(aggregateRoot.Forename.Value, forenameField.Value);
        
        // TODO: Remaining fields;
    }

    private void ValidateCommonAggregateProperties(Individual aggregateRoot, IndividualResource resource)
    {
        Assert.Equal(resource.Id, aggregateRoot.Id.Value);
        Assert.Equal(resource.Salutation, aggregateRoot.Salutation?.Value);
        Assert.Equal(resource.Forename, aggregateRoot.Forename);
        Assert.Equal(resource.Surname, aggregateRoot.Surname);
        foreach (EmailAddressResource emailAddress in resource.EmailAddresses)
        {
            Assert.Contains(aggregateRoot.EmailAddresses, x => x.Address == emailAddress.Address);
        }
        foreach (TelephoneNumberResource telephoneNumber in resource.TelephoneNumbers)
        {
            Assert.Contains(aggregateRoot.TelephoneNumbers, x => x.Number == telephoneNumber.Number);
        }
    }
}