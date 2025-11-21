using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Resolution;

public class ProductIdResolver : IProductIdResolver
{
    private readonly IEnumerable<IProductIdResolutionStrategy> _resolutionStrategies;

    public ProductIdResolver(IEnumerable<IProductIdResolutionStrategy> resolutionStrategies)
    {
        _resolutionStrategies = resolutionStrategies;
    }
        
    public Task<Maybe<Guid>> ResolveAsync() =>
        Task.FromResult(_resolutionStrategies.Coalesce(strategy => strategy.Resolve(), Maybe<Guid>.None));
}