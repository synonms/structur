using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Domain.ValueObjects;

public record Units : IntValueObject<Units>
{
    private const int DefaultMinValue = 0;
    private const int DefaultMaxValue = int.MaxValue;
    
    private Units(int value) : base(value)
    {
    }

    public static OneOf<Units, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, int value) =>
        CreateMandatory(propertyName, value, DefaultMinValue, DefaultMaxValue);

    public static OneOf<Units, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, int value, int minimumValue, int maximumValue) =>
        ValueObject.CreateBuilder<Units>()
            .WithFaultIfValueMoreThan(propertyName, value, maximumValue)
            .WithFaultIfValueLessThan(propertyName, value, minimumValue)
            .Build(value, x => new Units(x));

    public static OneOf<Maybe<Units>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, int? value)  =>
        CreateOptional(propertyName, value, DefaultMinValue, DefaultMaxValue);

    public static OneOf<Maybe<Units>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, int? value, int minimumValue, int maximumValue)
    {
        if (value is null)
        {
            return Maybe<Units>.None;
        }

        return CreateMandatory(propertyName, value.Value, minimumValue, maximumValue).ToMaybe();
    }

    public static Units Convert(int value) =>
        CreateMandatory(nameof(Units), value).Match(
            valueObject => valueObject,
            _ => new Units(DefaultMinValue));
}
