using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Domain.ValueObjects;

public record Postcode : StringValueObject<Postcode>
{
    private Postcode(string value) : base(value)
    {
    }

    public static OneOf<Postcode, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<Postcode>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.Postcode)
            .Build(value, x => new Postcode(x));

    public static OneOf<Maybe<Postcode>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<Postcode>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static Postcode Convert(string value) =>
        CreateMandatory(nameof(Postcode), value)
            .Match(
                valueObject => valueObject,
                _ => new Postcode(string.Empty));
}