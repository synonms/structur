using Synonms.Structur.Core.System;

namespace Synonms.Structur.Api.Core.Schema.Resources;

public abstract class ChildResource
{
    protected ChildResource()
    {
        Id = Guid.NewGuid().ToComb();
    }
    
    protected ChildResource(Guid id)
    {
        Id = id;
    }

    public Guid Id { get; init; }
    
    public ResourceLinks Links { get; } = new();
}