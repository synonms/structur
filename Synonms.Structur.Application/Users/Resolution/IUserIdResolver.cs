using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Users.Resolution;

public interface IUserIdResolver
{
    Task<Maybe<Guid>> ResolveAsync();
}