namespace Synonms.Structur.Api.Server.Routing;

public class RouteNameProvider : IRouteNameProvider
{
    public string GetById(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(GetById);

    public string GetAll(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(GetAll);

    public string Post(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(Post);

    public string Put(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(Put);

    public string Delete(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(Delete);

    public string CreateForm(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(CreateForm);

    public string EditForm(Type aggregateRootType, Type resourceType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + nameof(EditForm);

    public string GetProjection(Type aggregateRootType, Type resourceType, Type projectionType) =>
        GetRepresentationPrefix(aggregateRootType, resourceType) + projectionType.Name + nameof(GetProjection);

    private static string GetRepresentationPrefix(Type aggregateRootType, Type resourceType) =>
        aggregateRootType.Name + resourceType.FullName!.Replace('.', '_').Replace('+', '_');
}
