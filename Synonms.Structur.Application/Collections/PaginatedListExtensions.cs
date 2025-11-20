using Synonms.Structur.Application.Schema;
using Synonms.Structur.Core.Collections;

namespace Synonms.Structur.Application.Collections;

public static class PaginatedListExtensions
{
    public static Pagination GeneratePagination<T>(this PaginatedList<T> paginatedList, Func<int, Uri> resourceCollectionUriFunc) 
    {
        Link firstLink = paginatedList.GenerateFirstLink(resourceCollectionUriFunc);
        Link lastLink = paginatedList.GenerateLastLink(resourceCollectionUriFunc);
        Link? previousLink = paginatedList.GeneratePreviousLink(resourceCollectionUriFunc);
        Link? nextLink = paginatedList.GenerateNextLink(resourceCollectionUriFunc);
        
        return new Pagination(paginatedList.Offset, paginatedList.Limit, paginatedList.Size, firstLink, lastLink)
        {
            Previous = previousLink,
            Next = nextLink
        };
    }
    
    private static Link GenerateFirstLink<T>(this PaginatedList<T> paginatedList, Func<int, Uri> resourceCollectionUriFunc)
    {
        const int offset = 0;
        Uri firstUri = resourceCollectionUriFunc(offset);
        
        return Link.PageLink(firstUri);
    }

    private static Link GenerateLastLink<T>(this PaginatedList<T> paginatedList, Func<int, Uri> resourceCollectionUriFunc)
    {
        int offset = paginatedList.Limit <= 0 ? 0 : paginatedList.Size - (paginatedList.Size % paginatedList.Limit);
        Uri lastUri = resourceCollectionUriFunc(offset);
        
        return Link.PageLink(lastUri);
    }

    private static Link? GeneratePreviousLink<T>(this PaginatedList<T> paginatedList, Func<int, Uri> resourceCollectionUriFunc)
    {
        if (paginatedList.HasPrevious is false)
        {
            return null;
        }

        int offset = Math.Max(paginatedList.Offset - paginatedList.Limit, 0);
        Uri previousUri = resourceCollectionUriFunc(offset);
        
        return Link.PageLink(previousUri);
    }

    private static Link? GenerateNextLink<T>(this PaginatedList<T> paginatedList, Func<int, Uri> resourceCollectionUriFunc)
    {
        if (paginatedList.HasNext is false)
        {
            return null;
        }

        int offset = paginatedList.Offset + paginatedList.Limit;
        Uri nextUri = resourceCollectionUriFunc(offset);
        
        return Link.PageLink(nextUri);
    }
}