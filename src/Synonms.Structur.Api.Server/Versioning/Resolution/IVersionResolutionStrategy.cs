using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Versioning.Resolution;

public interface IVersionResolutionStrategy
{
    Maybe<Version> Resolve();
}