namespace Synonms.Structur.Presentation.Resources;

public abstract class Resource
{
}

public abstract class Resource<TId> : Resource
{
    public TId Id { get; init; }
}