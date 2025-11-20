using Synonms.Structur.Core.System;

namespace Synonms.Structur.Application.Schema.Resources;

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
    
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; init; }
}