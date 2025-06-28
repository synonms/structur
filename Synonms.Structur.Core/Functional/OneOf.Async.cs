namespace Synonms.Structur.Core.Functional;

public partial class OneOf<TLeft, TRight>
{
    public Task MatchAsync(Action<TLeft> leftAction, Action<TRight> rightAction)
    {
        if (IsLeft)
        {
            leftAction(_leftValue);
        }
        else
        {
            rightAction(_rightValue);
        }
            
        return Task.CompletedTask;
    }

    public async Task<TOut> MatchAsync<TOut>(Func<TLeft, Task<TOut>> leftFunc, Func<TRight, Task<TOut>> rightFunc) =>
        IsLeft 
            ? await leftFunc(_leftValue).ConfigureAwait(false) 
            : await rightFunc(_rightValue).ConfigureAwait(false);
}