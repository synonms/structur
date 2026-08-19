using Synonms.Structur.Api.Core.Iana;

namespace Synonms.Structur.Api.Core.Schema;

public class Document
{
    protected Document(Link selfLink)
    {
        Links[IanaLinkRelationConstants.Self] = selfLink;
    }

    public Dictionary<string, Link> Links { get; } = new();
}