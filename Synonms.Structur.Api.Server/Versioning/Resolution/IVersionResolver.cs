using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Versioning.Resolution;

public interface IVersionResolver
{
    Task<Maybe<Version>> ResolveAsync();
}