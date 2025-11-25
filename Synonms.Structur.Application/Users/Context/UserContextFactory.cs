using Synonms.Structur.Application.Users.Persistence;

namespace Synonms.Structur.Application.Users.Context;

public class UserContextFactory<TUser> : IUserContextFactory<TUser>
    where TUser : StructurUser
{
    private readonly IUserRepository<TUser> _repository;

    public UserContextFactory(IUserRepository<TUser> repository)
    {
        _repository = repository;
    }

    public async Task<UserContext<TUser>> CreateAsync(Guid? authenticatedUserId, CancellationToken cancellationToken)
    {
        if (authenticatedUserId is null)
        {
            return UserContext<TUser>.Create(null);
        }

        return (await _repository.FindAuthenticatedUserAsync(authenticatedUserId.Value, cancellationToken))
            .Match(
                UserContext<TUser>.Create,
                () => UserContext<TUser>.Create(null));
    }
}