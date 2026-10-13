using Synonms.Structur.Api.Core.Schema.Resources;

namespace Synonms.Structur.Api.Client.Http.Requests;

public class PutRequest<TResource>
{
    public Guid? TenantId { get; init; }
    public Guid? ProductId { get; init; }
    public required Guid Id { get; init; }
    public required TResource Resource { get; init; }
    public ResourceMetadata? Metadata { get; init; }
}
