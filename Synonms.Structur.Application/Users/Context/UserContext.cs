namespace Synonms.Structur.Application.Users.Context;

public class UserContext
{
    protected UserContext(StructurUser? authenticatedUser)
    {
        BaseAuthenticatedUser = authenticatedUser;
    }

    public StructurUser? BaseAuthenticatedUser { get; }
}

public class UserContext<TUser> : UserContext
    where TUser : StructurUser
{
    private UserContext(TUser? authenticatedUser) : base(authenticatedUser)
    {
        AuthenticatedUser = authenticatedUser;
    }
    
    public TUser? AuthenticatedUser { get; }

    public static UserContext<TUser> Create(TUser? authenticatedUser) =>
        new (authenticatedUser);
}