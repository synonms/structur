namespace Synonms.Structur.Api.Server.Mapping;

public interface IChildResourceMapperFactory
{
    IChildResourceMapper? Create(Type aggregateMemberType, Type childResourceType);
}