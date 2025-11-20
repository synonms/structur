namespace Synonms.Structur.Application.Mapping;

public interface IChildResourceMapperFactory
{
    IChildResourceMapper? Create(Type aggregateMemberType, Type childResourceType);
}