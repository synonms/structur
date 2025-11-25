using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Products.Persistence;

public class NoStructurProductRepository : IProductRepository<NoStructurProduct>
{
    public Task<IEnumerable<NoStructurProduct>> ReadAvailableProductsAsync(CancellationToken cancellationToken) =>
        Task.FromResult(Enumerable.Empty<NoStructurProduct>());

    public Task<Maybe<NoStructurProduct>> FindSelectedProductAsync(Guid id, CancellationToken cancellationToken) =>
        Maybe<NoStructurProduct>.NoneAsync;
}