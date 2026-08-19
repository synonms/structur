using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Users.Persistence;

public class NoStructurUserRepository : IUserRepository<NoStructurUser>
{
    public Task<Maybe<NoStructurUser>> FindAuthenticatedUserAsync(Guid id, CancellationToken cancellationToken) =>
        Maybe<NoStructurUser>.NoneAsync;
}