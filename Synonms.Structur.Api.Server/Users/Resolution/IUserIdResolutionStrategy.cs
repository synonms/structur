using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Users.Resolution;

public interface IUserIdResolutionStrategy
{
    Maybe<Guid> Resolve();
}