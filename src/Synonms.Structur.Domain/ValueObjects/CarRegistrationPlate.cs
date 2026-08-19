using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System.Text;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class CarRegistrationPlate : StringValueObject
{
    private CarRegistrationPlate(string value) : base(value)
    {
    }

    public static OneOf<CarRegistrationPlate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, string value) =>
        Validator.CreateBuilder<CarRegistrationPlate>()
            .WithFaultIfNotPopulated(propertyName, value)
            .WithFaultIfNotMatchingPattern(propertyName, value, RegularExpressions.CarRegistration)
            .Build(() => new CarRegistrationPlate(value));

    public static OneOf<Maybe<CarRegistrationPlate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Maybe<CarRegistrationPlate>.None;
        }

        return CreateMandatory(propertyName, value).ToMaybe();
    }

    public static CarRegistrationPlate Convert(string value) =>
        CreateMandatory(nameof(CarRegistrationPlate), value)
            .Match(
                valueObject => valueObject,
                _ => new CarRegistrationPlate(string.Empty));
}


