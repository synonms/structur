using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class Label : ValueObject<Label, string>, IValueObject<Label, string>
{
    public const int DefaultMaxLength = 250;
    
    private Label(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(Label valueObject) => valueObject.Value;

    public static OneOf<Label, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<Label, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<Label>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .Build(() => new Label(value));

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

    public static Label Rehydrate(string value) =>
        CreateMandatory(nameof(Label), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new Label(string.Empty));
}