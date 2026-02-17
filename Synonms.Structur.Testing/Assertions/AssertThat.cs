
using Synonms.Structur.Api.Core.Iana;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;

namespace Synonms.Structur.Testing.Assertions;

public static class AssertThat
{
    public static TResource Resource<TResource>(TResource resource) 
        where TResource : Resource => 
        resource;
        
    public static Dictionary<string, Link> Links(Dictionary<string, Link> links) => 
        links;

    public static IReadOnlyDictionary<string, Link> Links(IReadOnlyDictionary<string, Link> links) => 
        links;

    public static Dictionary<string, Link> LinksFromPagination(Pagination pagination)
    {
        Dictionary<string, Link> actualLinks = new Dictionary<string, Link>()
        {
            [IanaLinkRelationConstants.Pagination.First] = pagination.First,
            [IanaLinkRelationConstants.Pagination.Last] = pagination.Last
        };

        if (pagination.Previous is not null)
        {
            actualLinks[IanaLinkRelationConstants.Pagination.Previous] = pagination.Previous;
        }
        
        if (pagination.Next is not null)
        {
            actualLinks[IanaLinkRelationConstants.Pagination.Next] = pagination.Next;
        }

        return actualLinks;
    }
}