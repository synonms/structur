using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.Domain.ValueObjects;

public record EventDateTime : ValueObject<DateTime>, IComparable, IComparable<EventDateTime>
{
    private EventDateTime(DateTime value) : base(value)
    {
    }
        
    public static implicit operator DateTime(EventDateTime valueObject) => valueObject.Value;

    public static OneOf<EventDateTime, IEnumerable<DomainRuleFault>> CreateMandatory(DateTime value) =>
        CreateMandatory(value, DateTime.MinValue, DateTime.MaxValue);

    public static OneOf<EventDateTime, IEnumerable<DomainRuleFault>> CreateMandatory(DateTime value, DateTime minimum, DateTime maximum)
    {
        List<DomainRuleFault> faults = new ();

        if (value < minimum)
        {
            faults.Add(new DomainRuleFault("{0} property has minimum of {1}.", nameof(EventDateTime), minimum));
        }

        if (value > maximum)
        {
            faults.Add(new DomainRuleFault("{0} property has maximum of {1}.", nameof(EventDateTime), maximum));
        }
            
        return faults.Any() ? faults : new EventDateTime(value);
    }

    public static OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>> CreateOptional(DateTime? value) =>
        CreateOptional(value, DateTime.MinValue, DateTime.MaxValue);

    public static OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>> CreateOptional(DateTime? value, DateTime minimum, DateTime maximum)
    {
        if (value is null)
        {
            return new OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>>(Maybe<EventDateTime>.None);
        }

        return CreateMandatory(value.Value, minimum, maximum).Match(
            valueObject => new OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>>(valueObject), 
            faults => new OneOf<Maybe<EventDateTime>, IEnumerable<DomainRuleFault>>(faults));
    }
        
    internal static EventDateTime Convert(DateTime value) =>
        CreateMandatory(value, value, value).Match(
            valueObject => valueObject,
            fault => new EventDateTime(DateTime.MinValue)
        );
        
    public int CompareTo(EventDateTime? other) => DateTime.Compare(Value, other?.Value ?? DateTime.MinValue);
        
    public int CompareTo(object? obj) => Value.CompareTo(obj);
}