namespace Synonms.Structur.Core.Collections;

public class PaginatedList<T> : List<T>
{
    public int Offset { get; }
    
    public int Limit { get; }
    
    public int Size { get; }

    public PaginatedList(List<T> items, int offset, int limit, int size)
    {
        Offset = offset;
        Limit = limit;
        Size = size;

        AddRange(items);
    }

    public bool HasPrevious => Offset > 1;

    public bool HasNext => Limit > 0 && (Offset + Limit) < Size;

    public static PaginatedList<T> Create(IQueryable<T> source, int offset, int limit)
    {
        int size = source.Count();
        
        List<T> items = limit > 0
            ? source.Skip(offset).Take(limit).ToList()
            : source.Skip(offset).ToList();
        
        return new PaginatedList<T>(items, offset, limit, size);
    }

    public static PaginatedList<T> Create(IEnumerable<T> source, int offset, int limit, int size)
    {
        List<T> items = source.ToList();
        return new PaginatedList<T>(items, offset, limit, size);
    }
    
    public static PaginatedList<T> CreateEmpty(int limit)
    {
        List<T> items = [];
        return new PaginatedList<T>(items, 0, limit, 0);
    }
}