using Synonms.Structur.Api.Server.Pipeline;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Projections;

namespace Synonms.Structur.Api.Server.Routing;

public interface IRouteGenerator
{
    Uri Item<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;

    Uri Item(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null);

    Uri Item(Type aggregateRootType, Type resourceType, Guid id, QueryParameters? queryParameters = null);

    Uri Collection<TAggregateRoot>(QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;

    Uri Collection(Type aggregateRootType, QueryParameters? queryParameters = null);

    Uri Collection(Type aggregateRootType, Type resourceType, QueryParameters? queryParameters = null);

    Uri CreateForm<TAggregateRoot>(QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;

    Uri CreateForm(Type aggregateRootType, QueryParameters? queryParameters = null);

    Uri CreateForm(Type aggregateRootType, Type resourceType, QueryParameters? queryParameters = null);

    Uri EditForm<TAggregateRoot>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;

    Uri EditForm(Type aggregateRootType, Guid id, QueryParameters? queryParameters = null);

    Uri EditForm(Type aggregateRootType, Type resourceType, Guid id, QueryParameters? queryParameters = null);

    public Uri Projection<TAggregateRoot, TProjection>(EntityId<TAggregateRoot> id, QueryParameters? queryParameters = null)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
        where TProjection : Projection<TAggregateRoot>;

    public Uri Projection(Type aggregateRootType, Type projectionType, Guid id, QueryParameters? queryParameters = null);

    public Uri Projection(Type aggregateRootType, Type resourceType, Type projectionType, Guid id, QueryParameters? queryParameters = null);
}
