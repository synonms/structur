using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Tenants.Resolution;

public interface ITenantIdResolutionStrategy
{
    Maybe<Guid> Resolve();
}