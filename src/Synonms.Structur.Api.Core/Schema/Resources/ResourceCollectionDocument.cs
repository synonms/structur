using Synonms.Structur.Api.Core.Iana;

namespace Synonms.Structur.Api.Core.Schema.Resources;

public class ResourceCollectionDocument<TResource> : Document
{
    public ResourceCollectionDocument(Link selfLink, IEnumerable<ResourceEnvelope<TResource>> resources, Pagination pagination)
        : base(selfLink)
    {
        Envelopes = resources.ToArray();
        Resources = Envelopes.Select(envelope => envelope.Data).ToArray();
        Pagination = pagination;
    }

    public IEnumerable<TResource> Resources { get; }

    /// <summary>Per-item framework metadata where the document was assembled by a server mapper.</summary>
    public IReadOnlyList<ResourceEnvelope<TResource>> Envelopes { get; }

    public Pagination Pagination { get; }

    public ResourceCollectionDocument<TResource> WithLink(string name, Link link)
    {
        Links[name] = link;

        return this;
    }

    public ResourceCollectionDocument<TResource> WithCreateForm(Uri uri)
    {
        Links[IanaLinkRelationConstants.Forms.Create] = Link.CreateFormLink(uri);

        return this;
    }
}
