using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects.Abstractions;

namespace Synonms.Structur.Domain.ValueObjects;

public class EventDateTime : DateTimeValueObject
{
    private EventDateTime(DateTime value) : base(value)
    {
    }
        
    public static OneOf<EventDateTime, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateTime value) =>
        CreateMandatory(propertyName, value, DateTime.MinValue, DateTime.MaxValue);

    public static OneOf<EventDateTime, IEnumerable<DomainRuleFault>> CreateMandatory(string propertyName, DateTime value, DateTime minimum, DateTime maximum) =>
        Validator.CreateBuilder<EventDateTime>()
            .WithFaultIfValueLessThan(propertyName, value, minimum)
            .WithFaultIfValueMoreThan(propertyName, value, maximum)
            .Build(() => new EventDateTime(value));

    public static OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateTime? value) =>
        CreateOptional(propertyName, value, DateTime.MinValue, DateTime.MaxValue);

    public static OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>> CreateOptional(string propertyName, DateTime? value, DateTime minimum, DateTime maximum)
    {
        if (value is null)
        {
            return Maybe<EventDateTime>.None;
        }

        return CreateMandatory(propertyName, value.Value, minimum, maximum).ToMaybe();
    }
        
    internal static EventDateTime Convert(DateTime value) =>
        CreateMandatory(nameof(EventDateTime), value, value, value).Match(
            valueObject => valueObject,
            _ => new EventDateTime(DateTime.MinValue)
        );
}