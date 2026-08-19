namespace Synonms.Structur.Infrastructure.Persistence;

public class UserActionRecord
{
    public DateTime ActionAt { get; set; } = DateTime.MinValue;

    public Guid ActionById { get; set; } = Guid.Empty;
    
    public string ActionByName { get; set; } = string.Empty;
}