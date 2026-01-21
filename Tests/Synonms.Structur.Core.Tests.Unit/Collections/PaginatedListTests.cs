using Synonms.Structur.Core.Collections;
using Xunit;

namespace Synonms.Structur.Core.Tests.Unit.Collections;

public class PaginatedListTests
{
    [Fact]
    public void Create_EmptySource_CreatesEmptyList()
    {
        const int limit = 5;
        const int offset = 0;
        const int size = 0;
        
        IQueryable<int> source = new List<int>().AsQueryable();
        
        PaginatedList<int> list = PaginatedList<int>.Create(source, offset, limit);
        
        Assert.Equal(limit, list.Limit);
        Assert.Equal(offset, list.Offset);
        Assert.Equal(size, list.Size);
        Assert.False(list.HasPrevious);
        Assert.False(list.HasNext);
        Assert.Empty(list);
    }

    [Fact] 
    public void Create_SourceSmallerThanPageSize_CreatesListWithAllElements()
    {
        const int limit = 5;
        const int offset = 0;
        const int size = 2;
        
        IQueryable<int> source = new List<int>{ 1, 2 }.AsQueryable();
        
        PaginatedList<int> list = PaginatedList<int>.Create(source, offset, limit);
        
        Assert.Equal(limit, list.Limit);
        Assert.Equal(offset, list.Offset);
        Assert.Equal(size, list.Size);
        Assert.False(list.HasPrevious);
        Assert.False(list.HasNext);
        Assert.Collection(list,
            x => Assert.Equal(1, x),
            x => Assert.Equal(2, x));
    }

    [Fact] 
    public void Create_SourceLargerThanPageSizeWithNoOffset_CreatesListWithCorrectSubsetOfElements()
    {
        const int limit = 5;
        const int offset = 0;
        const int size = 12;
        
        IQueryable<int> source = new List<int>{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.AsQueryable();
        
        PaginatedList<int> list = PaginatedList<int>.Create(source, offset, limit);
        
        Assert.Equal(limit, list.Limit);
        Assert.Equal(offset, list.Offset);
        Assert.Equal(size, list.Size);
        Assert.False(list.HasPrevious);
        Assert.True(list.HasNext);
        Assert.Collection(list,
            x => Assert.Equal(1, x),
            x => Assert.Equal(2, x),
            x => Assert.Equal(3, x),
            x => Assert.Equal(4, x),
            x => Assert.Equal(5, x));
    }
    
    [Fact] 
    public void Create_SourceLargerThanPageSizeWithOffset_CreatesListWithCorrectSubsetOfElements()
    {
        const int limit = 5;
        const int offset = 5;
        const int size = 12;
        
        IQueryable<int> source = new List<int>{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.AsQueryable();
        
        PaginatedList<int> list = PaginatedList<int>.Create(source, offset, limit);
        
        Assert.Equal(limit, list.Limit);
        Assert.Equal(offset, list.Offset);
        Assert.Equal(size, list.Size);
        Assert.True(list.HasPrevious);
        Assert.True(list.HasNext);
        Assert.Collection(list,
            x => Assert.Equal(6, x),
            x => Assert.Equal(7, x),
            x => Assert.Equal(8, x),
            x => Assert.Equal(9, x),
            x => Assert.Equal(10, x));
    }
    
    [Fact] 
    public void Create_SourceLargerThanPageSizeWithOffsetWithinFinalPage_CreatesListWithCorrectSubsetOfElements()
    {
        const int limit = 5;
        const int offset = 10;
        const int size = 12;
        
        IQueryable<int> source = new List<int>{ 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }.AsQueryable();
        
        PaginatedList<int> list = PaginatedList<int>.Create(source, offset, limit);
        
        Assert.Equal(limit, list.Limit);
        Assert.Equal(offset, list.Offset);
        Assert.Equal(size, list.Size);
        Assert.True(list.HasPrevious);
        Assert.False(list.HasNext);
        Assert.Collection(list,
            x => Assert.Equal(11, x),
            x => Assert.Equal(12, x));
    }
}