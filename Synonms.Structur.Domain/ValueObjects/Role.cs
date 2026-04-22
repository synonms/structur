using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class Role : StringValueObject
{
    private Role(string value) : base(value)
    {
    }

    public static OneOf<Role, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<Role>()
            .WithFaultIfNotPopulated(propertyName, value)
            .Build(() => new Role(value));

    public static OneOf<Maybe<Role>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Role>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static Role Convert(string value) =>
        CreateMandatory(nameof(Role), value)
            .Match(
                valueObject => valueObject,
                _ => new Role(string.Empty));
}

