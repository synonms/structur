namespace Synonms.Structur.Api.Client.Http.Responses;

public class PostResponse
{
    public required Guid Id { get; init; }
    public required Guid EntityTag { get; init; }
}