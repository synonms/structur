using Synonms.Structur.Api.Core.Schema.Resources;

namespace Synonms.Structur.Api.Client.Http.Requests;

public class PostRequest<TResource> where TResource : Resource, new()
{
    public Guid? TenantId { get; init; }
    public Guid? ProductId { get; init; }
    public required TResource Resource { get; init; }
}