using System.Linq.Expressions;

namespace Synonms.Structur.Domain.Aggregates;

/// <summary>An immutable, non-executable description of a bounded aggregate read.</summary>
public sealed class AggregateQuery<TAggregateRoot>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    public const int MaximumTake = 1000;

    private AggregateQuery(Expression<Func<TAggregateRoot, bool>>? predicate,
        IReadOnlyList<AggregateQuerySort<TAggregateRoot>> sortKeys, int skip, int? take)
    {
        Predicate = predicate;
        SortKeys = sortKeys;
        SkipCount = skip;
        TakeCount = take;
    }

    public Expression<Func<TAggregateRoot, bool>>? Predicate { get; }

    public IReadOnlyList<AggregateQuerySort<TAggregateRoot>> SortKeys { get; }

    public int SkipCount { get; }

    public int? TakeCount { get; }

    public static AggregateQuery<TAggregateRoot> All { get; } =
        new(null, Array.Empty<AggregateQuerySort<TAggregateRoot>>(), 0, null);

    public AggregateQuery<TAggregateRoot> Where(Expression<Func<TAggregateRoot, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (Predicate is null)
        {
            return new AggregateQuery<TAggregateRoot>(predicate, SortKeys, SkipCount, TakeCount);
        }

        ParameterExpression parameter = Expression.Parameter(typeof(TAggregateRoot), "aggregate");
        Expression left = new ParameterReplacementVisitor(Predicate.Parameters[0], parameter).Visit(Predicate.Body)!;
        Expression right = new ParameterReplacementVisitor(predicate.Parameters[0], parameter).Visit(predicate.Body)!;
        Expression<Func<TAggregateRoot, bool>> combined =
            Expression.Lambda<Func<TAggregateRoot, bool>>(Expression.AndAlso(left, right), parameter);
        return new AggregateQuery<TAggregateRoot>(combined, SortKeys, SkipCount, TakeCount);
    }

    public AggregateQuery<TAggregateRoot> OrderBy<TKey>(Expression<Func<TAggregateRoot, TKey>> key, bool descending = false) =>
        AddSort(key, descending, then: false);

    public AggregateQuery<TAggregateRoot> ThenBy<TKey>(Expression<Func<TAggregateRoot, TKey>> key, bool descending = false) =>
        AddSort(key, descending, then: true);

    public AggregateQuery<TAggregateRoot> Skip(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return new AggregateQuery<TAggregateRoot>(Predicate, SortKeys, count, TakeCount);
    }

    public AggregateQuery<TAggregateRoot> Take(int count)
    {
        if (count is < 1 or > MaximumTake)
        {
            throw new ArgumentOutOfRangeException(nameof(count), $"Take must be between 1 and {MaximumTake}.");
        }

        return new AggregateQuery<TAggregateRoot>(Predicate, SortKeys, SkipCount, count);
    }

    private AggregateQuery<TAggregateRoot> AddSort<TKey>(Expression<Func<TAggregateRoot, TKey>> key, bool descending, bool then)
    {
        ArgumentNullException.ThrowIfNull(key);
        AggregateQuerySort<TAggregateRoot>[] next = then
            ? SortKeys.Append(new AggregateQuerySort<TAggregateRoot>(key, descending)).ToArray()
            : [new AggregateQuerySort<TAggregateRoot>(key, descending)];
        return new AggregateQuery<TAggregateRoot>(Predicate, Array.AsReadOnly(next), SkipCount, TakeCount);
    }

    private sealed class ParameterReplacementVisitor(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : node;
    }
}

public sealed record AggregateQuerySort<TAggregateRoot>(LambdaExpression Key, bool Descending)
    where TAggregateRoot : AggregateRoot<TAggregateRoot>;
