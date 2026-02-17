using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;

namespace Synonms.Structur.Domain.ValueObjects;

public record NationalInsuranceNumber : StringValueObject<NationalInsuranceNumber>
{
    public const int ValidLength = 9;

    private NationalInsuranceNumber(string value) : base(value)
    {
    }

    public static implicit operator string(NationalInsuranceNumber friendlyId) => friendlyId.Value;

    public static OneOf<NationalInsuranceNumber, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        ValueObject.CreateBuilder<NationalInsuranceNumber>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfLengthNot(propertyName, value, ValidLength)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.NationalInsuranceNumber)
            .Build(value, x => new NationalInsuranceNumber(x));

    public static OneOf<Maybe<NationalInsuranceNumber>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<NationalInsuranceNumber>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static NationalInsuranceNumber Convert(string value) =>
        CreateMandatory(nameof(NationalInsuranceNumber), value)
            .Match(
                valueObject => valueObject,
                _ => new NationalInsuranceNumber(string.Empty));
}
