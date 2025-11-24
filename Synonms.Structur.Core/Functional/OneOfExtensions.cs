namespace Synonms.Structur.Core.Functional;

public static class OneOfExtensions
{
    public static IEnumerable<TLeft> Lefts<TLeft, TRight>(this IEnumerable<OneOf<TLeft, TRight>> oneOfs) =>
        oneOfs.Where(x => x.IsLeft)
            .SelectMany(x => x.LeftAsEnumerable());
        
    public static IEnumerable<TRight> Rights<TLeft, TRight>(this IEnumerable<OneOf<TLeft, TRight>> oneOfs) =>
        oneOfs.Where(x => x.IsRight)
            .SelectMany(x => x.RightAsEnumerable());
}