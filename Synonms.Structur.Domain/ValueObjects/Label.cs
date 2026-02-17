using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public record Label : StringValueObject<Label>
{
    private Label(string value) : base(value)
    {
    }

    public static OneOf<Label, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Label, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        ValueObject.CreateBuilder<Label>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(value, x => new Label(x));

    public static OneOf<Maybe<Label>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)  =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<Label>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Label>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static Label Convert(string value) =>
        CreateMandatory(nameof(Label), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new Label(string.Empty));
}