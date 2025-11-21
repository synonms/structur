namespace Synonms.Structur.Application.Users.Context;

public interface IUserContextFactory<TUser>
    where TUser : StructurUser
{
    Task<UserContext<TUser>> CreateAsync(Guid? authenticatedUserId, CancellationToken cancellationToken);
}