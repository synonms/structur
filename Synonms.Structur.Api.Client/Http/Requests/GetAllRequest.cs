namespace Synonms.Structur.Api.Client.Http.Requests;

public class GetAllRequest
{
    public Guid? TenantId { get; init; }
    public Guid? ProductId { get; init; }
    public int? Offset { get; init; }
    public int? Limit { get; init; }
    public Dictionary<string, string> QueryParameters { get; init; } = new();
}