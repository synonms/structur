namespace Synonms.Structur.Application.Schema;

public class Pagination
{
    public const int DefaultPageLimit = 25;

    public Pagination(int offset, int limit, int size, Link first, Link last)
    {
        Offset = offset;
        Limit = limit;
        Size = size;
        First = first;
        Last = last;
    }

    /// <summary>
    /// Number of elements to skip.
    /// </summary>
    public int Offset { get; }
    
    /// <summary>
    /// Maximum number of elements per page.
    /// </summary>
    public int Limit { get; }
    
    /// <summary>
    /// Total number of elements.
    /// </summary>
    public int Size { get; } 
    
    public Link First { get; }
    
    public Link Last { get; }
    
    public Link? Previous { get; init; }
    
    public Link? Next { get; init; }
}