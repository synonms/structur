namespace Synonms.Structur.Api.Core.Schema.Resources;

/// <summary>
/// Carries consumer-owned resource data and framework-owned transport metadata separately.
/// </summary>
public sealed class ResourceEnvelope<TResource>
{
    public ResourceEnvelope(TResource data, ResourceMetadata metadata)
    {
        Data = data;
        Metadata = metadata;
    }

    public TResource Data { get; }

    public ResourceMetadata Metadata { get; }
}
