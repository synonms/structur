using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Users.Persistence;

public interface IUserRepository<TUser>
    where TUser : StructurUser
{
    Task<Maybe<TUser>> FindAuthenticatedUserAsync(Guid id, CancellationToken cancellationToken);
}