using Synonms.Structur.Domain.ValueObjects;

namespace Synonms.Structur.Application.Tests.Unit.Shared;

internal static class TestUser
{
    public static readonly UserActionDto UserActionDto = new()
    {
        ActionAt = DateTime.UtcNow,
        ActionById = Guid.NewGuid(),
        ActionByName = "Test"
    };

    public static readonly UserAction UserAction = UserAction.CreateMandatory("UserAction", UserActionDto)
        .Match(userAction => userAction, domainRuleFaults => throw new Exception("Unable to create UserAction."));

}