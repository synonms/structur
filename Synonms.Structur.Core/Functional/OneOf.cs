namespace Synonms.Structur.Core.Functional;

public partial class OneOf<TLeft, TRight>
{
    private readonly TLeft _leftValue;
    private readonly TRight _rightValue;

    public OneOf(TLeft leftValue)
    {
        if (leftValue is null)
        {
            throw new ArgumentNullException(nameof(leftValue));
        }

        _leftValue = leftValue;
        _rightValue = default!;
        IsLeft = true;
    }
        
    public OneOf(TRight rightValue)
    {
        if (rightValue is null)
        {
            throw new ArgumentNullException(nameof(rightValue));
        }
            
        _leftValue = default!;
        _rightValue = rightValue;
        IsLeft = false;
    }

    public bool IsLeft { get; }

    public bool IsRight => !IsLeft;

    public static explicit operator TLeft(OneOf<TLeft, TRight> oneOf) =>
        oneOf.IsLeft
            ? oneOf._leftValue
            : throw new InvalidCastException("OneOf is not in a Left state");

    public static explicit operator TRight(OneOf<TLeft, TRight> oneOf) =>
        oneOf.IsRight
            ? oneOf._rightValue
            : throw new InvalidCastException("OneOf is not in a Right state");

    public static implicit operator OneOf<TLeft, TRight>(TLeft leftValue) => new (leftValue);
        
    public static implicit operator OneOf<TLeft, TRight>(TRight rightValue) => new (rightValue);

    public void Match(Action<TLeft> leftAction, Action<TRight> rightAction)
    {
        if (IsLeft)
        {
            leftAction(_leftValue);
        }
        else
        {
            rightAction(_rightValue);
        }
    }

    public TOut Match<TOut>(Func<TLeft, TOut> leftFunc, Func<TRight, TOut> rightFunc) =>
        IsLeft 
            ? leftFunc(_leftValue) 
            : rightFunc(_rightValue);
        
    public IEnumerable<TLeft> LeftAsEnumerable() =>
        IsLeft
            ? new[] {_leftValue}
            : Enumerable.Empty<TLeft>();

    public IEnumerable<TRight> RightAsEnumerable() =>
        IsLeft
            ? Enumerable.Empty<TRight>()
            : new[] {_rightValue};

    public override int GetHashCode() =>
        IsLeft
            ? EqualityComparer<TLeft>.Default.GetHashCode(_leftValue!)
            : EqualityComparer<TRight>.Default.GetHashCode(_rightValue!);
        
    public override string ToString() =>
        IsLeft
            ? $"Left({_leftValue?.ToString() ?? "null"})"
            : $"Right({_rightValue?.ToString() ?? "null"})";
}