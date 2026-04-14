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

    public virtual SortedSet<Version> SupportedVersions { get; } = [];

    public Version? GetApplicableVersion(Version? requestedVersion)
    {
        if (requestedVersion is null) return new Version();
        if (SupportedVersions.Count == 0 || requestedVersion > SupportedVersions.Last()) return new Version();
        if (SupportedVersions.Contains(requestedVersion)) return requestedVersion;
        
        return SupportedVersions.Where(x => x < requestedVersion).Max();
    }

    public Guid Id { get; set; }

    public Link SelfLink { get; init; }

    public ResourceLinks Links { get; } = new();
}
