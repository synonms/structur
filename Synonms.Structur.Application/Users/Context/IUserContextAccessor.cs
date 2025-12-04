namespace Synonms.Structur.Application.Users.Context;

public interface IUserContextAccessor
{
    UserContext? BaseUserContext { get; set; }
}

public interface IUserContextAccessor<TUser> : IUserContextAccessor
    where TUser : StructurUser
{
    UserContext<TUser>? UserContext { get; set; } 
}