using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Core.Schema.Client;

public class ResourceCollectionResponse<TResource> : OneOf<ErrorCollectionDocument, ResourceCollectionDocument<TResource>>

{
    public ResourceCollectionResponse(ErrorCollectionDocument leftValue) : base(leftValue)
    {
    }

    public ResourceCollectionResponse(ResourceCollectionDocument<TResource> rightValue) : base(rightValue)
    {
    }
}
