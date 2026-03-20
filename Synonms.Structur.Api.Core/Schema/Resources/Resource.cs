using Synonms.Structur.Core.System;

namespace Synonms.Structur.Api.Core.Schema.Resources;

public abstract class Resource
{
    protected Resource()
    {
        Id = Guid.NewGuid().ToComb();
        SelfLink = Link.SelfLink(new Uri("/" + Id, UriKind.Relative));
    }
    
    protected Resource(Guid id, Link selfLink)
    {
        Id = id;
        SelfLink = selfLink;
        string relativeResourcePath = selfLink.Uri.IsAbsoluteUri ? selfLink.Uri.AbsolutePath : selfLink.Uri.OriginalString;
        Links.Add("projections", Link.ProjectionsLink(new Uri(relativeResourcePath + "/projections", UriKind.Relative)));
    }

    public abstract string GetCollectionPath();
    
    public Guid Id { get; set; }

    public Link SelfLink { get; init; }

    public ResourceLinks Links { get; } = new();
}
