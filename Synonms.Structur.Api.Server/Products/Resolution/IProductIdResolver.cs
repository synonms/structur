using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Products.Resolution;

public interface IProductIdResolver
{
    Task<Maybe<Guid>> ResolveAsync();
}