using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Domain.ValueObjects;

public record AddressLine : StringValueObject<AddressLine>
{
    private AddressLine(string value) : base(value)
    {
    }

    public static OneOf<AddressLine, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        CreateMandatory(propertyName, value, DefaultMaxLength);

    public static OneOf<AddressLine, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value, int maxLength) =>
        ValueObject.CreateBuilder<AddressLine>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthMoreThan(propertyName, value, maxLength)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.AddressLine)
            .Build(value, x => new AddressLine(x));

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

    public static AddressLine Convert(string value) =>
        CreateMandatory(nameof(AddressLine), value, value.Length)
            .Match(
                valueObject => valueObject,
                _ => new AddressLine(string.Empty));
}