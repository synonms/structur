namespace Synonms.Structur.Core.Functional;

public static class EnumerableExtensions
{
    public static Result<IEnumerable<T>> ToResult<T>(this IEnumerable<T> enumerable) =>
        new (enumerable);

    public static Maybe<TMaybe> Coalesce<TEnumerable, TMaybe>(this IEnumerable<TEnumerable> enumerable, Func<TEnumerable, Maybe<TMaybe>> func, Maybe<TMaybe> fallback)
    {
        foreach (TEnumerable item in enumerable)
        {
            Maybe<TMaybe> result = func.Invoke(item);

            if (result.IsSome)
            {
                return result;
            }
        }

        return fallback;
    }
}