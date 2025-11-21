using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Users.Persistence;

public interface IUserRepository<TUser>
    where TUser : StructurUser
{
    Task<Maybe<TUser>> FindAuthenticatedUserAsync(CancellationToken cancellationToken);
}