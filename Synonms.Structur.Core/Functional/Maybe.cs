namespace Synonms.Structur.Core.Functional;

public readonly partial struct Maybe<T>
{
    private readonly T _value;
        
    public Maybe(T value)
    {
        _value = value;
        IsNone = value is null || value.Equals(default(T));
    }

    public bool IsNone { get; }

    public bool IsSome => !IsNone;

    public static Maybe<T> Some(T value) => new (value);
        
    public static readonly Maybe<T> None = new(default(T)!);

    public static explicit operator T(Maybe<T> maybe) =>
        maybe.IsSome 
            ? maybe._value 
            : throw new InvalidCastException("Option is not in Some state");

    public static implicit operator Maybe<T>(T value) => 
        new (value);

    public static bool operator ==(Maybe<T> left, Maybe<T> right) => 
        left.Equals(right);

    public static bool operator !=(Maybe<T> left, Maybe<T> right) => 
        !left.Equals(right);
        
    public IEnumerable<T> AsEnumerable() =>
        IsSome
            ? new[] {_value}
            : Enumerable.Empty<T>();

    public Maybe<TOut> Bind<TOut>(Func<T, Maybe<TOut>> someFunc) =>
        IsSome
            ? someFunc(_value)
            : Maybe<TOut>.None;

    public Maybe<TOut> BiBind<TOut>(Func<T, Maybe<TOut>> someFunc, Func<Maybe<TOut>> noneFunc) =>
        IsSome
            ? someFunc(_value)
            : noneFunc();

    public T Coalesce(T fallback) =>
        IsSome 
            ? _value 
            : fallback;

    public T Coalesce(Func<T> fallbackFunc) =>
        IsSome 
            ? _value 
            : fallbackFunc.Invoke();

    public IEnumerable<Maybe<T>> Collect(Func<Maybe<T>> function) =>
        new List<Maybe<T>>
        {
            this,
            function.Invoke()
        };

    public IEnumerable<Maybe<T>> Collect(Maybe<T> maybe) =>
        new List<Maybe<T>>
        {
            this,
            maybe
        };

    public Maybe<T> Filter(Func<T, bool> predicate) =>
        IsSome && predicate(_value)
            ? this
            : None;

    public Maybe<T> IfNone(Action noneAction)
    {
        if (IsNone)
        {
            noneAction();
        }

        return this;
    }

    public Maybe<T> IfSome(Action<T> someAction)
    {
        if (IsSome)
        {
            someAction(_value);
        }

        return this;
    }

    public void Match(Action<T> someAction, Action noneAction)
    {
        if (IsSome)
        {
            someAction(_value);
        }
        else
        {
            noneAction();
        }
    }

    public TOut Match<TOut>(Func<T, TOut> someFunc, Func<TOut> noneFunc) =>
        IsSome 
            ? someFunc(_value) 
            : noneFunc();

    public Maybe<TOut> Map<TOut>(Func<T, TOut> projectionFunc) =>
        IsSome
            ? projectionFunc(_value)
            : Maybe<TOut>.None;

    public override bool Equals(object? obj) =>
        obj is Maybe<T> maybe && Equals(maybe);

    public bool Equals(Maybe<T> other) =>
        IsSome && other.IsSome 
            ? Equals(_value, other._value) 
            : IsSome == other.IsSome;

    public override int GetHashCode() =>
        IsSome
            ? EqualityComparer<T>.Default.GetHashCode(_value!)
            : 0;
        
    public override string ToString() =>
        IsSome
            ? $"Some({_value?.ToString()})"
            : "None";
}