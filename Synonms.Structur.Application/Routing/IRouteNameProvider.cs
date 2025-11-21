using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Routing;

public interface IRouteNameProvider
{
    string GetById<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string GetById(Type aggregateRootType);
    
    string GetAll<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string GetAll(Type aggregateRootType);
    
    string Post<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string Post(Type aggregateRootType);
    
    string Put<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string Put(Type aggregateRootType);
    
    string Delete<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string Delete(Type aggregateRootType);
    
    string CreateForm<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string CreateForm(Type aggregateRootType);
    
    string EditForm<TAggregateRoot>()
        where TAggregateRoot : AggregateRoot<TAggregateRoot>;
        
    string EditForm(Type aggregateRootType);
}