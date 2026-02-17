namespace Synonms.Structur.Api.Server.Mapping;

public interface IResourceMapperFactory
{
    IResourceMapper? Create(Type aggregateRootType, Type resourceType);
}