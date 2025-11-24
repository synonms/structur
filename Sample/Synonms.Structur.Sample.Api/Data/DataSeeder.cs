using MongoDB.Driver;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Widgets.Presentation;

namespace Synonms.Structur.Sample.Api.Data;

public partial class DataSeeder
{
    private IMongoCollection<DomainEvent>? _domainEventsCollection;
    private IMongoCollection<Widget>? _widgetsCollection;

    public async Task SeedDevelopmentDataAsync(WebApplication webApplication)
    {
        await using AsyncServiceScope serviceScope = webApplication.Services.CreateAsyncScope();
        IMongoClient mongoClient = serviceScope.ServiceProvider.GetRequiredService<IMongoClient>();

        SetCollections(mongoClient);

        await SeedWidgetsAsync();
    }

    private void SetCollections(IMongoClient mongoClient)
    {
        _domainEventsCollection ??= mongoClient.GetDatabase("synonms-structur-sample-mongodb")
            .GetCollection<DomainEvent>("domain-events");

        _widgetsCollection ??= mongoClient.GetDatabase("synonms-structur-sample-mongodb")
            .GetCollection<Widget>("widgets");
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

}
