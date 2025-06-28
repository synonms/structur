namespace Synonms.Structur.Core.Functional;

public readonly partial struct Maybe<T>
{
    public static Task<Maybe<T>> SomeAsync(T value) => Task.FromResult(new Maybe<T>(value));
        
    public static async Task<Maybe<T>> SomeAsync(Task<T> value) => new (await value);
        
    public static readonly Task<Maybe<T>> NoneAsync = Task.FromResult(new Maybe<T>(default(T)!));

    public Task<Maybe<T>> AsAsync() => 
        Task.FromResult(this);

    public async Task<Maybe<TOut>> BindAsync<TOut>(Func<T, Task<Maybe<TOut>>> projectionFunc) =>
        IsSome
            ? await projectionFunc(_value).ConfigureAwait(false) 
            : Maybe<TOut>.None;

    public async Task<Maybe<TOut>> BiBindAsync<TOut>(Func<T, Task<Maybe<TOut>>> someFunc, Func<Task<Maybe<TOut>>> noneFunc) =>
        IsSome
            ? await someFunc(_value).ConfigureAwait(false) 
            : await noneFunc().ConfigureAwait(false);

    public async Task<T> CoalesceAsync(Task<T> fallback) =>
        IsSome 
            ? _value 
            : await fallback;

    public async Task<T> CoalesceAsync(Func<Task<T>> fallbackFunc) =>
        IsSome 
            ? _value 
            : await fallbackFunc.Invoke().ConfigureAwait(false);

    public async Task<IEnumerable<Maybe<T>>> CollectAsync(Task<Maybe<T>> maybe) =>
        new List<Maybe<T>>
        {
            this,
            await maybe
        };

    public async Task<IEnumerable<Maybe<T>>> CollectAsync(Func<Task<Maybe<T>>> function) =>
        new List<Maybe<T>>
        {
            this,
            await function.Invoke().ConfigureAwait(false)
        };

    public Task IfNoneAsync(Action noneAction)
    {
        if (IsSome)
        {
            noneAction();
        }
            
        return Task.CompletedTask;
    }

    public Task IfSomeAsync(Action<T> someAction)
    {
        if (IsSome)
        {
            someAction(_value);
        }

        return Task.CompletedTask;
    }

    public async Task<Maybe<T>> FilterAsync(Func<T, Task<bool>> predicate) =>
        IsSome && await predicate(_value).ConfigureAwait(false)
            ? this
            : None;

    public async Task<Maybe<TOut>> MapAsync<TOut>(Func<T, Task<TOut>> projectionFunc) =>
        IsSome
            ? await projectionFunc(_value).ConfigureAwait(false)
            : Maybe<TOut>.None;

    public Task MatchAsync(Action<T> someAction, Action noneAction)
    {
        if (IsSome)
        {
            someAction(_value);
        }
        else
        {
            noneAction();
        }
            
        return Task.CompletedTask;
    }

    public Task MatchAsync(Func<T, Task> someFunc, Func<Task> noneFunc) =>
        IsSome
            ? someFunc(_value)
            : noneFunc();

    public async Task<TOut> MatchAsync<TOut>(Func<T, Task<TOut>> someFunc, Func<Task<TOut>> noneFunc) =>
        IsSome 
            ? await someFunc(_value).ConfigureAwait(false) 
            : await noneFunc().ConfigureAwait(false);
}