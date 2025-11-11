using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Core.Functional;

public static partial class MaybeExtensions
{
    public static Maybe<Fault> BiBind(this Maybe<Fault> maybe, Func<Maybe<Fault>> noneFunc) =>
        maybe.BiBind(Maybe<Fault>.Some, noneFunc);

    public static Maybe<TOut> Reduce<T, TOut>(this IEnumerable<Maybe<T>> maybes, Func<IEnumerable<T>, TOut> projectionFunc) =>
        maybes.Any(x => x.IsSome)
            ? projectionFunc.Invoke(maybes.Where(x => x.IsSome).SelectMany(x => x.AsEnumerable()))
            : Maybe<TOut>.None;
        
    public static IEnumerable<Maybe<T>> Collect<T>(this IEnumerable<Maybe<T>> maybes, Func<Maybe<T>> function) =>
        maybes.Append(function.Invoke());
        
    public static Result<T> ToResult<T>(this Maybe<Fault> maybe, T value) =>
        maybe.Match(
            Result<T>.Failure,
            () => Result<T>.Success(value));

    public static Result<T> ToResult<T>(this Maybe<Fault> maybe, Result<T> result) =>
        maybe.Match(
            Result<T>.Failure,
            () => result);

    public static Result<T> ToResult<T>(this Maybe<Fault> maybe, Func<T> function) =>
        maybe.Match(
            Result<T>.Failure,
            () => Result<T>.Success(function.Invoke()));

    public static Result<T> ToResult<T>(this Maybe<Fault> maybe, Func<Result<T>> function) =>
        maybe.Match(
            Result<T>.Failure,
            function.Invoke);
}