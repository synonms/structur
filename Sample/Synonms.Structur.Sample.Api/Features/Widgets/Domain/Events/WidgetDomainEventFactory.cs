using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Widgets.Presentation;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.Sample.Api.Features.Widgets.Domain.Events;

public class WidgetDomainEventFactory : IDomainEventFactory<Widget, WidgetResource>
{
    public DomainEvent<Widget> GenerateCreatedEvent(WidgetResource resource) =>
        new WidgetCreatedEvent((EntityId<Widget>)resource.Id, resource);

    public DomainEvent<Widget> GenerateDeletedEvent(EntityId<Widget> aggregateId) =>
        new WidgetDeletedEvent(aggregateId);

    public DomainEvent<Widget> GenerateUpdatedEvent(WidgetResource resource) =>
        new WidgetUpdatedEvent((EntityId<Widget>)Guid.NewGuid(), resource);
}