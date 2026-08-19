using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Products.Resolution;

public interface IProductIdResolutionStrategy
{
    Maybe<Guid> Resolve();
}