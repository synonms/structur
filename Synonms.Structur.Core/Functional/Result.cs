using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Core.Functional;

public partial class Result<TSuccess> : OneOf<TSuccess, Fault>
{
    public Result(TSuccess success) : base(success)
    {
    }

    public Result(Fault fault) : base(fault)
    {
    }

    public bool IsSuccess => IsLeft;

    public bool IsFailure => IsRight;

    public static implicit operator Result<TSuccess>(TSuccess success) => new (success);

    public static implicit operator Result<TSuccess>(Fault fault) => new (fault);

    public static Result<TSuccess> Success(TSuccess success) => new (success);

    public static Result<TSuccess> Failure(Fault fault) => new (fault);
        
    public Result<TOut> Bind<TOut>(Func<TSuccess, Result<TOut>> successFunc) =>
        Match(successFunc, error => error);

    public Maybe<Fault> Bind(Func<TSuccess, Maybe<Fault>> successFunc) =>
        Match(successFunc, error => error);
}