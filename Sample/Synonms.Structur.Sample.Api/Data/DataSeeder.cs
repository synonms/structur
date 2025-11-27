using MongoDB.Driver;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Widgets.Presentation;

namespace Synonms.Structur.Sample.Api.Data;

public class DataSeeder
{
    private IMongoCollection<DomainEvent>? _domainEventsCollection;
    private IMongoCollection<Widget>? _widgetsCollection;
    private IMongoCollection<Individual>? _individualsCollection;

    public async Task SeedDevelopmentDataAsync(WebApplication webApplication, bool clearData = true)
    {
        await using AsyncServiceScope serviceScope = webApplication.Services.CreateAsyncScope();
        IMongoClient mongoClient = serviceScope.ServiceProvider.GetRequiredService<IMongoClient>();

        SetCollections(mongoClient);

        if (clearData)
        {
            await ClearDataAsync();
        }

        await SeedWidgetsAsync();
        await SeedIndividualsAsync();
    }

    private void SetCollections(IMongoClient mongoClient)
    {
        _domainEventsCollection ??= mongoClient.GetDatabase("synonms-structur-sample-mongodb")
            .GetCollection<DomainEvent>("domain-events");

        _widgetsCollection ??= mongoClient.GetDatabase("synonms-structur-sample-mongodb")
            .GetCollection<Widget>("widgets");
        
        _individualsCollection ??= mongoClient.GetDatabase("synonms-structur-sample-mongodb")
            .GetCollection<Individual>("individuals");
    }

    private async Task ClearDataAsync()
    {
        await _domainEventsCollection.DeleteManyAsync(x => true);
        await _widgetsCollection.DeleteManyAsync(x => true);
        await _individualsCollection.DeleteManyAsync(x => true);
    }
    
    private async Task SeedWidgetsAsync()
    {
        Guid widget1Id = Guid.Parse("e3fbfa59-d501-4532-ad8f-372d9cf6d5c3");
        Guid widget2Id = Guid.Parse("c76ed3ae-1bad-4b0b-8f91-456fa993fd33");
        
        WidgetCreatedEvent widget1CreatedEvent = new ((EntityId<Widget>)widget1Id, new WidgetResource(widget1Id, Link.EmptyLink()){ Name = "Widget 1"});
        WidgetCreatedEvent widget2CreatedEvent = new ((EntityId<Widget>)widget2Id, new WidgetResource(widget2Id, Link.EmptyLink()){ Name = "Widget 2"});
        
        await CreateWidgetAsync(widget1CreatedEvent);
        await CreateWidgetAsync(widget2CreatedEvent);
    }

    private async Task CreateWidgetAsync(WidgetCreatedEvent createdEvent)
    {
        Result<Widget> createdResult = await createdEvent.ApplyAsync(null);
            
        await createdResult.MatchAsync(
            async createdWidget =>
            {
                Widget? existingWidget = await _widgetsCollection
                    .Find(x => x.Id == createdEvent.AggregateId)
                    .FirstOrDefaultAsync(CancellationToken.None);

                if (existingWidget is null && _domainEventsCollection is not null && _widgetsCollection is not null)
                {
                    await _domainEventsCollection.InsertOneAsync(createdEvent);
                    await _widgetsCollection.InsertOneAsync(createdWidget);
                }
            },
            errors => throw new ApplicationException($"Unable to create widget Id '{createdEvent.AggregateId}': {errors}"));
    }

    private async Task SeedIndividualsAsync()
    {
        IndividualResource individual1Resource = new(Guid.Parse("a9617306-ffa6-4355-9461-9dfcd6b702d4"), Link.EmptyLink())
        {
            TenantReference = "REF0001",
            Forename = "Lebron",
            Surname = "James",
            EmailAddresses = 
            [
                new EmailAddressResource
                {
                    Address = "l.james@lakers.com",
                    IsPrimary = true
                }
            ],
            TelephoneNumbers = []
        };
        IndividualResource individual2Resource = new(Guid.Parse("294af0a0-0050-4562-8301-8a059bffefba"), Link.EmptyLink())
        {
            TenantReference = "REF0002",
            Forename = "Luka",
            Surname = "Doncic",
            EmailAddresses = [
                new EmailAddressResource
                {
                    Address = "l.doncic@lakers.com",
                    IsPrimary = true
                }
            ],
            TelephoneNumbers = []
        };
        
        IndividualCreatedEvent individual1CreatedEvent = new((EntityId<Individual>)individual1Resource.Id, individual1Resource);
        IndividualCreatedEvent individual2CreatedEvent = new((EntityId<Individual>)individual2Resource.Id, individual2Resource);
        
        await CreateIndividualAsync(individual1CreatedEvent);
        await CreateIndividualAsync(individual2CreatedEvent);
    }

    private async Task CreateIndividualAsync(IndividualCreatedEvent createdEvent)
    {
        Result<Individual> createdResult = await createdEvent.ApplyAsync(null);
            
        await createdResult.MatchAsync(
            async createdIndividual =>
            {
                Individual? existingIndividual = await _individualsCollection
                    .Find(x => x.Id == createdEvent.AggregateId)
                    .FirstOrDefaultAsync(CancellationToken.None);

                if (existingIndividual is null && _domainEventsCollection is not null && _individualsCollection is not null)
                {
                    await _domainEventsCollection.InsertOneAsync(createdEvent);
                    await _individualsCollection.InsertOneAsync(createdIndividual);
                }
            },
            errors => throw new ApplicationException($"Unable to create widget Id '{createdEvent.AggregateId}': {errors}"));
    }
}
