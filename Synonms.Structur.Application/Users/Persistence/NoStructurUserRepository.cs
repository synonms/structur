using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Users.Persistence;

public class NoStructurUserRepository : IUserRepository<NoStructurUser>
{
    public Task<Maybe<NoStructurUser>> FindAuthenticatedUserAsync(Guid id, CancellationToken cancellationToken) =>
        Maybe<NoStructurUser>.NoneAsync;
}