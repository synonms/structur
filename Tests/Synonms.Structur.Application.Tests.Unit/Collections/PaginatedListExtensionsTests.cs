using Synonms.Structur.Application.Collections;
using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Tests.Unit.Shared;
using Synonms.Structur.Core.Collections;
using Synonms.Structur.Domain.Entities;
using Xunit;

namespace Synonms.Structur.Application.Tests.Unit.Collections;

public class PaginatedListExtensionsTests
{
    private const string CollectionPath = "tests";
    
    [Fact]
    public void GeneratePagination_SetsCorrectProperties()
    {
        List<TestAggregateRoot> aggregateRoots =
        [
            new(EntityId<TestAggregateRoot>.New(), TestUser.UserAction),
            new(EntityId<TestAggregateRoot>.New(), TestUser.UserAction),
            new(EntityId<TestAggregateRoot>.New(), TestUser.UserAction),
            new(EntityId<TestAggregateRoot>.New(), TestUser.UserAction)
        ];
        // 10 in total: 2 skipped, 4 this page, 4 next page
        PaginatedList<TestAggregateRoot> paginatedList = new (aggregateRoots, 2, 4, 10);

        Uri PaginationUriFunc(int offset)
        {
            string queryString = offset > 0 ? "?offset=" + offset : string.Empty;
            return new Uri(CollectionPath + queryString, UriKind.Relative);
        }
        
        Pagination pagination = paginatedList.GeneratePagination(PaginationUriFunc);
        
        Assert.Equal(2, pagination.Offset);
        Assert.Equal(4, pagination.Limit);
        Assert.Equal(10, pagination.Size);
        Assert.Equal(CollectionPath, pagination.First.Uri.OriginalString);
        Assert.Equal(CollectionPath + "?offset=8", pagination.Last.Uri.OriginalString);
        Assert.NotNull(pagination.Previous);
        Assert.Equal(CollectionPath, pagination.Previous?.Uri.OriginalString);
        Assert.NotNull(pagination.Next);
        Assert.Equal(CollectionPath + "?offset=6", pagination.Next?.Uri.OriginalString);
    }
}