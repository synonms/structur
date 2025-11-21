using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Resolution;

public interface IProductIdResolver
{
    Task<Maybe<Guid>> ResolveAsync();
}