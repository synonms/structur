using Synonms.Structur.Application.Iana;

namespace Synonms.Structur.Application.Schema;

public class Document
{
    protected Document(Link selfLink)
    {
        Links[IanaLinkRelationConstants.Self] = selfLink;
    }

    public Dictionary<string, Link> Links { get; } = new();
}