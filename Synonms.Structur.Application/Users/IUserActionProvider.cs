using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Application.Users;

public interface IUserActionProvider
{
    UserActionDto Get();
}