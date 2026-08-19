namespace Synonms.Structur.Api.Client.Http.Requests;

public class DeleteRequest
{
    public Guid? TenantId { get; init; }
    public Guid? ProductId { get; init; }
    public required Guid Id { get; init; }
}