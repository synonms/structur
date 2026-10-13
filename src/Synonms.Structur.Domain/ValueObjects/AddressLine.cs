using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class AddressLine : ScalarValueObject<AddressLine, string>, IScalarValueObject<AddressLine, string>
{
    private const int DefaultMaxLength = 100;
    
    private AddressLine(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(AddressLine valueObject) => valueObject.Value;

    public static OneOf<AddressLine, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<AddressLine, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        Validator.CreateBuilder<AddressLine>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.AddressLine)
            .Build(() => new AddressLine(value));

    public static OneOf<Maybe<AddressLine>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)  =>
        CreateOptional(propertyName, value, DefaultMaxLength);

    public static OneOf<Maybe<AddressLine>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<AddressLine>.None;
        }

        return CreateMandatory(propertyName, value, maxLength).ToMaybe();
    }

    public static AddressLine Rehydrate(string value) =>
        CreateMandatory(nameof(AddressLine), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new AddressLine(value));
}