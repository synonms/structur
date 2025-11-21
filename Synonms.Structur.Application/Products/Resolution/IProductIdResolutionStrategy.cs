using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Resolution;

public interface IProductIdResolutionStrategy
{
    Maybe<Guid> Resolve();
}