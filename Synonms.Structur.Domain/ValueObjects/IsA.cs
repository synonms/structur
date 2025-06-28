using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.Domain.ValueObjects;

public record IsA : ValueObject<bool>, IComparable, IComparable<IsA>
{
    private IsA(bool value) : base(value)
    {
    }
    
    public static implicit operator bool(IsA valueObject) => valueObject.Value;

    public static IsA Yes => new IsA(true); 
    public static IsA No => new IsA(false);
    
    public static OneOf<IsA, IEnumerable<DomainRuleFault>> CreateMandatory(bool value) => new IsA(value);

    public static OneOf<Maybe<IsA>, IEnumerable<DomainRuleFault>> CreateOptional(bool? value)
    {
        if (value is null)
        {
            return new OneOf<Maybe<IsA>, IEnumerable<DomainRuleFault>>(Maybe<IsA>.None);
        }

        return CreateMandatory(value.Value).Match(
            valueObject => new OneOf<Maybe<IsA>, IEnumerable<DomainRuleFault>>(valueObject), 
            faults => new OneOf<Maybe<IsA>, IEnumerable<DomainRuleFault>>(faults));
    }
    
    internal static IsA Convert(bool value) =>
        CreateMandatory(value).Match(
            valueObject => valueObject,
            fault => new IsA(false)
        );

    public int CompareTo(IsA? other)
    {
        if (other is null)
        {
            return 1;
        }
        
        if (Value)
        {
            return other.Value ? 0 : 1;
        }
        
        if (other.Value) return -1;
        
        return 0;
    }
    
    public int CompareTo(object? obj) => Value.CompareTo(obj);
}