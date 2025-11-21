using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Tenants.Resolution;

public interface ITenantIdResolutionStrategy
{
    Maybe<Guid> Resolve();
}