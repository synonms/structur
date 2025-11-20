namespace Synonms.Structur.Application.Mapping;

public interface IResourceMapperFactory
{
    IResourceMapper? Create(Type aggregateRootType, Type resourceType);
}