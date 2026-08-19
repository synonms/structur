using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Versioning.Resolution;

public class VersionResolver : IVersionResolver
{
    private readonly IEnumerable<IVersionResolutionStrategy> _resolutionStrategies;

    public VersionResolver(IEnumerable<IVersionResolutionStrategy> resolutionStrategies)
    {
        _resolutionStrategies = resolutionStrategies;
    }
        
    public Task<Maybe<Version>> ResolveAsync() =>
        Task.FromResult(_resolutionStrategies.Coalesce(strategy => strategy.Resolve(), Maybe<Version>.None));
}