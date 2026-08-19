using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Core.Functional;

public static class OneOfExtensions
{
    public static IEnumerable<TLeft> Lefts<TLeft, TRight>(this IEnumerable<OneOf<TLeft, TRight>> oneOfs) =>
        oneOfs.Where(x => x.IsLeft)
            .SelectMany(x => x.LeftAsEnumerable());
        
    public static IEnumerable<TRight> Rights<TLeft, TRight>(this IEnumerable<OneOf<TLeft, TRight>> oneOfs) =>
        oneOfs.Where(x => x.IsRight)
            .SelectMany(x => x.RightAsEnumerable());
    
    public static OneOf<TOut, IEnumerable<DomainRuleFault>> Reduce<T, TOut>(this IEnumerable<OneOf<T, IEnumerable<DomainRuleFault>>> results, Func<IEnumerable<T>, TOut> projectionFunc)
    {
        List<DomainRuleFault> failures = results.Rights().SelectMany(x => x).ToList();
        return failures.Any()
            ? failures
            : projectionFunc.Invoke(results.Lefts());
    }
}