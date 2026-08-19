using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class UkBuildingSocietyRollNumber : StringValueObject
{
    private UkBuildingSocietyRollNumber(string value) : base(value)
    {
    }

    public static OneOf<UkBuildingSocietyRollNumber, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<UkBuildingSocietyRollNumber>()
            .WithFaultIfNotPopulated(propertyName, value)
            .Build(() => new UkBuildingSocietyRollNumber(value));

    public static OneOf<Maybe<UkBuildingSocietyRollNumber>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<UkBuildingSocietyRollNumber>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static UkBuildingSocietyRollNumber Convert(string value) =>
        CreateMandatory(nameof(UkBuildingSocietyRollNumber), value)
            .Match(
                valueObject => valueObject,
                _ => new UkBuildingSocietyRollNumber(string.Empty));
}

