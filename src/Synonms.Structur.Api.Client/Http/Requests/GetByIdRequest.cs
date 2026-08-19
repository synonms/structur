namespace Synonms.Structur.Api.Client.Http.Requests;

public class GetByIdRequest
{
    public Guid? TenantId { get; init; }
    public Guid? ProductId { get; init; }
    public required Guid Id { get; init; }
}