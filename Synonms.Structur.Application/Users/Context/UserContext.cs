namespace Synonms.Structur.Application.Users.Context;

public class UserContext<TUser>
    where TUser : StructurUser
{
    private UserContext(TUser? authenticatedUser)
    {
        AuthenticatedUser = authenticatedUser;
    }
    
    public TUser? AuthenticatedUser { get; }

    public static UserContext<TUser> Create(TUser? authenticatedUser) =>
        new (authenticatedUser);
}