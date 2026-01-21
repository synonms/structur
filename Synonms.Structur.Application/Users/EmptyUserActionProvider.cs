using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Application.Users;

public class EmptyUserActionProvider : IUserActionProvider
{
    public UserActionDto Get() => new()
    {
        ActionAt = DateTime.UtcNow,
        ActionById = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        ActionByName = "Default"
    };
}