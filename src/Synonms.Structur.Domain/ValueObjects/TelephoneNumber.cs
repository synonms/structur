using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class TelephoneNumber : ScalarValueObject<TelephoneNumber, string>, IScalarValueObject<TelephoneNumber, string>
{
    private TelephoneNumber(string value)
    {
        Value = value;
    }

    public override string Value { get; }

    public static implicit operator string(TelephoneNumber valueObject) => valueObject.Value;

    public static OneOf<TelephoneNumber, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<TelephoneNumber>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.TelephoneNumber)
            .Build(() => new TelephoneNumber(value));

    public static OneOf<Maybe<TelephoneNumber>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<TelephoneNumber>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static TelephoneNumber Rehydrate(string value) =>
        CreateMandatory(nameof(TelephoneNumber), value)
            .Match(
                valueObject => valueObject,
                _ => new TelephoneNumber(string.Empty));
}