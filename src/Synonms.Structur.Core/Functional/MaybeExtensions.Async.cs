using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Core.Functional;

public static partial class MaybeExtensions
{
    public static Task<Maybe<Fault>> BiBindAsync(this Maybe<Fault> maybe, Func<Task<Maybe<Fault>>> noneFunc) =>
            maybe.BiBindAsync(Maybe<Fault>.SomeAsync, noneFunc);

        public static async Task<Maybe<Fault>> BiBindAsync(this Task<Maybe<Fault>> maybeTask, Func<Maybe<Fault>> noneFunc) =>
            (await maybeTask).BiBind(Maybe<Fault>.Some, noneFunc);

        public static async Task<Maybe<Fault>> BiBindAsync(this Task<Maybe<Fault>> maybeTask, Func<Task<Maybe<Fault>>> noneFunc) =>
            await (await maybeTask)
                .MatchAsync(
                    Maybe<Fault>.SomeAsync,
                    async () => await noneFunc.Invoke().ConfigureAwait(false))
                .ConfigureAwait(false);

        public static async Task<Maybe<Fault>> BiBindAsync(this Func<Task<Maybe<Fault>>> maybeFunc, Func<Maybe<Fault>> noneFunc) =>
            (await maybeFunc.Invoke()).BiBind(Maybe<Fault>.Some, noneFunc);

        public static async Task<Maybe<Fault>> BiBindAsync(this Func<Task<Maybe<Fault>>> maybeFunc, Func<Task<Maybe<Fault>>> noneFunc) =>
            await (await maybeFunc.Invoke())
                .MatchAsync(
                    Maybe<Fault>.SomeAsync,
                    async () => await noneFunc.Invoke().ConfigureAwait(false))
                .ConfigureAwait(false);

        public static async Task<Maybe<TOut>> BindAsync<T, TOut>(this Task<Maybe<T>> maybeTask, Func<T, Maybe<TOut>> projectionFunc) =>
            (await maybeTask).Bind(projectionFunc);

        public static async Task<Maybe<TOut>> BindAsync<T, TOut>(this Task<Maybe<T>> maybeTask, Func<T, Task<Maybe<TOut>>> projectionFunc) =>
            await (await maybeTask).BindAsync(projectionFunc).ConfigureAwait(false);

        public static async Task<Maybe<TOut>> BindAsync<T, TOut>(this Func<Task<Maybe<T>>> maybeFunc, Func<T, Maybe<TOut>> projectionFunc) =>
            (await maybeFunc.Invoke()).Bind(projectionFunc);

        public static async Task<Maybe<TOut>> BindAsync<T, TOut>(this Func<Task<Maybe<T>>> maybeFunc, Func<T, Task<Maybe<TOut>>> projectionFunc) =>
            await (await maybeFunc.Invoke()).BindAsync(projectionFunc).ConfigureAwait(false);

        public static async Task<T> CoalesceAsync<T>(this Task<Maybe<T>> maybeTask, Task<T> fallback) =>
            await (await maybeTask).CoalesceAsync(fallback);

        public static async Task<T> CoalesceAsync<T>(this Task<Maybe<T>> maybeTask, Func<Task<T>> fallbackFunc) =>
            await (await maybeTask).CoalesceAsync(fallbackFunc);

        public static async Task<T> CoalesceAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Task<T> fallback) =>
            await (await maybeFunc.Invoke()).CoalesceAsync(fallback);

        public static async Task<T> CoalesceAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Func<Task<T>> fallbackFunc) =>
            await (await maybeFunc.Invoke()).CoalesceAsync(fallbackFunc);

        public static async Task<IEnumerable<Maybe<T>>> CollectAsync<T>(this Task<Maybe<T>> maybeTask, Task<Maybe<T>> maybe) => 
            await (await maybeTask).CollectAsync(maybe);

        public static async Task<IEnumerable<Maybe<T>>> CollectAsync<T>(this Task<Maybe<T>> maybeTask, Func<Task<Maybe<T>>> maybeFunc) =>
            await (await maybeTask).CollectAsync(maybeFunc);

        public static async Task<IEnumerable<Maybe<T>>> CollectAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Task<Maybe<T>> maybe) => 
            await (await maybeFunc.Invoke()).CollectAsync(maybe);

        public static async Task<IEnumerable<Maybe<T>>> CollectAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Func<Task<Maybe<T>>> function) =>
            await (await maybeFunc.Invoke()).CollectAsync(function);

        public static async Task IfNoneAsync<T>(this Task<Maybe<T>> maybeTask, Action noneAction) =>
            await (await maybeTask).IfNoneAsync(noneAction);

        public static async Task IfNoneAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Action noneAction) =>
            await (await maybeFunc.Invoke()).IfNoneAsync(noneAction);

        public static async Task IfSomeAsync<T>(this Task<Maybe<T>> maybeTask, Action<T> someAction) =>
            await (await maybeTask).IfSomeAsync(someAction);

        public static async Task IfSomeAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Action<T> someAction) =>
            await (await maybeFunc.Invoke()).IfSomeAsync(someAction);

        public static async Task<Maybe<T>> FilterAsync<T>(this Task<Maybe<T>> maybeTask, Func<T, Task<bool>> predicate) =>
            await (await maybeTask).FilterAsync(predicate);

        public static async Task<Maybe<T>> FilterAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Func<T, Task<bool>> predicate) =>
            await (await maybeFunc.Invoke()).FilterAsync(predicate);

        public static async Task<Maybe<TOut>> MapAsync<T, TOut>(this Task<Maybe<T>> maybeTask, Func<T, Task<TOut>> projectionFunc) =>
            await (await maybeTask).MapAsync(projectionFunc);

        public static async Task<Maybe<TOut>> MapAsync<T, TOut>(this Func<Task<Maybe<T>>> maybeFunc, Func<T, Task<TOut>> projectionFunc) =>
            await (await maybeFunc.Invoke()).MapAsync(projectionFunc);

        public static async Task MatchAsync<T>(this Task<Maybe<T>> maybeTask, Action<T> someAction, Action noneAction) =>
            await (await maybeTask).MatchAsync(someAction, noneAction);

        public static async Task MatchAsync<T>(this Task<Maybe<T>> maybeTask, Func<T, Task> someFunc, Func<Task> noneFunc) =>
            await (await maybeTask).MatchAsync(someFunc, noneFunc);

        public static async Task<TOut> MatchAsync<T, TOut>(this Task<Maybe<T>> maybeTask, Func<T, Task<TOut>> someFunc, Func<Task<TOut>> noneFunc) =>
            await (await maybeTask).MatchAsync(someFunc, noneFunc);

        public static async Task MatchAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Action<T> someAction, Action noneAction) =>
            await (await maybeFunc.Invoke()).MatchAsync(someAction, noneAction);

        public static async Task MatchAsync<T>(this Func<Task<Maybe<T>>> maybeFunc, Func<T, Task> someFunc, Func<Task> noneFunc) =>
            await (await maybeFunc.Invoke()).MatchAsync(someFunc, noneFunc);

        public static async Task<TOut> MatchAsync<T, TOut>(this Func<Task<Maybe<T>>> maybeFunc, Func<T, Task<TOut>> someFunc, Func<Task<TOut>> noneFunc) =>
            await (await maybeFunc.Invoke()).MatchAsync(someFunc, noneFunc);

        public static Task<Result<T>> ToResultAsync<T>(this Maybe<Fault> maybe, Func<Task<T>> function) =>
            maybe.MatchAsync(
                Result<T>.FailureAsync,
                async () => await Result<T>.SuccessAsync(await function.Invoke().ConfigureAwait(false)));

        public static Task<Result<T>> ToResultAsync<T>(this Maybe<Fault> maybe, Func<Task<Result<T>>> function) =>
            maybe.MatchAsync(
                Result<T>.FailureAsync,
                async () => await function.Invoke().ConfigureAwait(false));

        public static async Task<Result<T>> ToResultAsync<T>(this Task<Maybe<Fault>> maybeTask, Func<Task<T>> function) =>
            await (await maybeTask).ToResultAsync(function);

        public static async Task<Result<T>> ToResultAsync<T>(this Task<Maybe<Fault>> maybeTask, Func<Task<Result<T>>> function) =>
            await (await maybeTask).ToResultAsync(function);
        
        public static async Task<Result<T>> ToResultAsync<T>(this Func<Task<Maybe<Fault>>> maybeFunc, Func<Task<T>> function) =>
            await (await maybeFunc.Invoke()).ToResultAsync(function);

        public static async Task<Result<T>> ToResultAsync<T>(this Func<Task<Maybe<Fault>>> maybeFunc, Func<Task<Result<T>>> function) =>
            await (await maybeFunc.Invoke()).ToResultAsync(function);
}