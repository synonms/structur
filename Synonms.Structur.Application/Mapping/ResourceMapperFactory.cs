using Microsoft.Extensions.DependencyInjection;

namespace Synonms.Structur.Application.Mapping;

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

        Type closedGenericMapperType = typeof(IResourceMapper<,>).MakeGenericType(aggregateRootType, resourceType);

        IResourceMapper? mapper = resourceMappers.FirstOrDefault(x => x.GetType().IsAssignableTo(closedGenericMapperType));

        return mapper;
    }
}