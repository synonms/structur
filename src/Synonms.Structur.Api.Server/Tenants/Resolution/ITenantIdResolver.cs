using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Server.Tenants.Resolution;

public interface ITenantIdResolver
{
    Task<Maybe<Guid>> ResolveAsync();
}