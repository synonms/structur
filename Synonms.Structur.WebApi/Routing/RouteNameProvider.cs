using Synonms.Structur.Application.Routing;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Routing;

public class RouteNameProvider : IRouteNameProvider
{
    public string GetById<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        GetById(typeof(TAggregateRoot));

    public string GetById(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(GetById);
    
    public string GetAll<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        GetAll(typeof(TAggregateRoot));

    public string GetAll(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(GetAll);

    public string Post<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        Post(typeof(TAggregateRoot));
        
    public string Post(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(Post);
    
    public string Put<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        Post(typeof(TAggregateRoot));
        
    public string Put(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(Put);
    
    public string Delete<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        Post(typeof(TAggregateRoot));
        
    public string Delete(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(Delete);

    public string CreateForm<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        CreateForm(typeof(TAggregateRoot));

    public string CreateForm(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(CreateForm);

    public string EditForm<TAggregateRoot>() where TAggregateRoot : AggregateRoot<TAggregateRoot> =>
        EditForm(typeof(TAggregateRoot));

    public string EditForm(Type aggregateRootType) =>
        aggregateRootType.Name + nameof(EditForm);
}