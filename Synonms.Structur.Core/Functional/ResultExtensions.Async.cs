using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Core.Functional;

public static partial class ResultExtensions
{
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Result<TOut>>> successFunc) =>
        await (await resultTask)
            .MatchAsync(
                async value => await successFunc.Invoke(value).ConfigureAwait(false), 
                Result<TOut>.FailureAsync)
            .ConfigureAwait(false);
        
    public static async Task<Maybe<Fault>> BindAsync<TIn>(this Task<Result<TIn>> resultTask, Func<TIn, Task<Maybe<Fault>>> successFunc) =>
        await (await resultTask)
            .MatchAsync(
                async value => await successFunc.Invoke(value).ConfigureAwait(false), 
                Maybe<Fault>.SomeAsync)
            .ConfigureAwait(false);
        
    public static async Task<Result<TOut>> BindAsync<TIn, TOut>(this Func<Task<Result<TIn>>> resultFunc, Func<TIn, Task<Result<TOut>>> successFunc) =>
        await (await resultFunc.Invoke())
            .MatchAsync(
                async value => await successFunc.Invoke(value).ConfigureAwait(false), 
                Result<TOut>.FailureAsync)
            .ConfigureAwait(false);
        
    public static async Task<Maybe<Fault>> BindAsync<TIn>(this Func<Task<Result<TIn>>> resultFunc, Func<TIn, Task<Maybe<Fault>>> successFunc) =>
        await (await resultFunc.Invoke())
            .MatchAsync(
                async value => await successFunc.Invoke(value).ConfigureAwait(false), 
                Maybe<Fault>.SomeAsync)
            .ConfigureAwait(false);
}