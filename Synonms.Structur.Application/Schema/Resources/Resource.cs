using Synonms.Structur.Core.System;

namespace Synonms.Structur.Application.Schema.Resources;

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
    }
    
    public Guid Id { get; set; }
    
    // public bool IsDeleted { get; init; }
    //
    // public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    //
    // public DateTime? UpdatedAt { get; init; }

    public Link SelfLink { get; init; }

    public ResourceLinks Links { get; } = new();
}