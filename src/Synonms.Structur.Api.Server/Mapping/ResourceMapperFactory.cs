using Microsoft.Extensions.DependencyInjection;

namespace Synonms.Structur.Api.Server.Mapping;

public class ResourceMapperFactory : IResourceMapperFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ResourceMapperFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IResourceMapper? Create(Type aggregateRootType, Type resourceType)
    {
        IEnumerable<IResourceMapper> resourceMappers = _serviceProvider.GetRequiredService<IEnumerable<IResourceMapper>>();

        Type contract = typeof(IResourceMapper<,>).MakeGenericType(aggregateRootType, resourceType);
        return MapperResolution.Resolve(resourceMappers, contract, typeof(DefaultResourceMapper<,>), aggregateRootType, resourceType);
    }
}
