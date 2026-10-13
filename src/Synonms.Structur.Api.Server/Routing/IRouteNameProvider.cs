namespace Synonms.Structur.Api.Server.Routing;

public interface IRouteNameProvider
{
    string GetById(Type aggregateRootType, Type resourceType);

    string GetAll(Type aggregateRootType, Type resourceType);

    string Post(Type aggregateRootType, Type resourceType);

    string Put(Type aggregateRootType, Type resourceType);

    string Delete(Type aggregateRootType, Type resourceType);

    string CreateForm(Type aggregateRootType, Type resourceType);

    string EditForm(Type aggregateRootType, Type resourceType);

    string GetProjection(Type aggregateRootType, Type resourceType, Type projectionType);
}
