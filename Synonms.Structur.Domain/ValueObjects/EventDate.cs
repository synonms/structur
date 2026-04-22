using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EventDate : DateOnlyValueObject
{
    private EventDate(DateOnly value) : base(value)
    {
    }
        
    public static OneOf<EventDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value) =>
        CreateMandatory(propertyName, value, DateOnly.MinValue, DateOnly.MaxValue);

    public static OneOf<EventDate, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateOnly value, DateOnly minimum, DateOnly maximum) =>
        Validator.CreateBuilder<EventDate>()
            .WithFaultIfValueLessThan(propertyName, value, minimum)
            .WithFaultIfValueMoreThan(propertyName, value, maximum)
            .Build(() => new EventDate(value));

    public static OneOf<Maybe<EventDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value) =>
        CreateOptional(propertyName, value, DateOnly.MinValue, DateOnly.MaxValue);

    public static OneOf<Maybe<EventDate>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateOnly? value, DateOnly minimum, DateOnly maximum)
    {
        if (value is null)
        {
            return Maybe<EventDate>.None;
        }

        return CreateMandatory(propertyName, value.Value, minimum, maximum).ToMaybe();
    }
        
    public static EventDate Convert(DateOnly value) =>
        CreateMandatory(nameof(EventDate), value, value, value).Match(
            valueObject => valueObject,
            _ => new EventDate(DateOnly.MinValue)
        );
}