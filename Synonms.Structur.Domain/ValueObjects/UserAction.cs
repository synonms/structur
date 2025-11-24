using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.Domain.Validation;

namespace Synonms.Structur.Domain.ValueObjects;

public class UserActionDto
{
    public required DateTime ActionAt { get; init; }
    public required Guid ActionById { get; init; }
    public required string ActionByName { get; init; }
}

public record UserAction : ValueObject<UserAction>
{
    private UserAction(DateTime actionAt, Guid actionById, string actionByName)
    {
        ActionAt = actionAt;
        ActionById = actionById;
        ActionByName = actionByName;
    }

    public DateTime ActionAt { get; private set; }
    public Guid ActionById { get; private set; }
    public string ActionByName { get; private set; }

    public static OneOf<UserAction, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, UserActionDto dto) =>
        ValueObject.CreateBuilder<UserAction>()
            .WithFaultIfNotPopulated($"{propertyName}.{nameof(ActionAt)}", dto.ActionAt)
            .WithFaultIfNotPopulated($"{propertyName}.{nameof(ActionById)}", dto.ActionById)
            .WithFaultIfNotPopulated($"{propertyName}.{nameof(ActionByName)}", dto.ActionByName)
            .Build(dto, x => new UserAction(dto.ActionAt, dto.ActionById, dto.ActionByName));

    public static OneOf<Maybe<UserAction>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, UserActionDto? dto)
    {
        if (dto is null)
        {
            return Maybe<UserAction>.None;
        }

        return CreateMandatory(propertyName, dto).ToMaybe();
    }

    public override int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is UserAction other)
        {
            return CompareTo(other);
        }

        return 0;
    }

    public override int CompareTo(UserAction? other) => ActionAt.CompareTo(other?.ActionAt ?? DateTime.MinValue);
}