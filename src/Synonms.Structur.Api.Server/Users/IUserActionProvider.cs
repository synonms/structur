using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Api.Server.Users;

public interface IUserActionProvider
{
    UserActionDto Get();
}