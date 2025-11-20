using Synonms.Structur.Application.Iana;

namespace Synonms.Structur.Application.Schema.Resources;

public class ResourceDocument<TResource> : Document
    where TResource : Resource
{
    public ResourceDocument(Link selfLink, TResource resource) 
        : base(selfLink)
    {
        Resource = resource;
    }
    
    public TResource Resource { get; }
    
    public ResourceDocument<TResource> WithLink(string name, Link link)
    {
        Links[name] = link;

        return this;
    }    
    
    public ResourceDocument<TResource> WithEditForm(Uri uri)
    {
        Links[IanaLinkRelationConstants.Forms.Edit] = Link.EditFormLink(uri);

        return this;
    }
    
    public ResourceDocument<TResource> WithDelete(Uri uri)
    {
        Links[LinkRelationConstants.Delete] = new Link(uri, IanaLinkRelationConstants.Self, IanaHttpMethodConstants.Delete);

        return this;
    }
}