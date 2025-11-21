using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Application.Tenants.Resolution;

public interface ITenantIdResolver
{
    Task<Maybe<Guid>> ResolveAsync();
}