using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Users.Resolution;

public interface IUserIdResolutionStrategy
{
    Maybe<Guid> Resolve();
}