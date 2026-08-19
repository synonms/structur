using Synonms.Structur.Api.Core.Schema.Errors;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Api.Core.Schema.Client;

public class ResourceResponse<TResource> : OneOf<ErrorCollectionDocument, ResourceDocument<TResource>>
    where TResource : Resource
{
    public ResourceResponse(ErrorCollectionDocument leftValue) : base(leftValue)
    {
    }

    public ResourceResponse(ResourceDocument<TResource> rightValue) : base(rightValue)
    {
    }
}