using Microsoft.Extensions.DependencyInjection;

namespace Synonms.Structur.Api.Server.Mapping;

public class ChildResourceMapperFactory : IChildResourceMapperFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ChildResourceMapperFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IChildResourceMapper? Create(Type aggregateMemberType, Type childResourceType)
    {
        IEnumerable<IChildResourceMapper> childResourceMappers = _serviceProvider.GetRequiredService<IEnumerable<IChildResourceMapper>>();

        Type contract = typeof(IChildResourceMapper<,>).MakeGenericType(aggregateMemberType, childResourceType);
        return MapperResolution.Resolve(childResourceMappers, contract, typeof(DefaultChildResourceMapper<,>), aggregateMemberType, childResourceType);
    }
}
