namespace Synonms.Structur.Application.Users.Context;

public class UserContextAccessor<TUser> : IUserContextAccessor<TUser>
    where TUser : StructurUser
{
    public UserContext<TUser>? UserContext { get; set; }
}