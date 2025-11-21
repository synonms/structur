namespace Synonms.Structur.Application.Users.Context;

public interface IUserContextAccessor<TUser>
    where TUser : StructurUser
{
    UserContext<TUser>? UserContext { get; set; } 
}