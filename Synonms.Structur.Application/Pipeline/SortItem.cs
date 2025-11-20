namespace Synonms.Structur.Application.Pipeline;

public class SortItem
{
    public enum SortDirection
    {
        Ascending = 0,
        Descending
    }
        
    public SortDirection Direction { get; init; }

    public string PropertyName { get; init; } = string.Empty;
}