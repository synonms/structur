namespace Synonms.Structur.Api.Core.Schema.Resources;

/// <summary>
/// Framework-owned metadata associated with a resource representation.
/// This metadata is deliberately not part of the consumer-owned data contract.
/// </summary>
public sealed class ResourceMetadata
{
    private readonly Dictionary<object, ResourceMetadata> _embeddedMetadata = new(ReferenceEqualityComparer.Instance);
    private Dictionary<string, Guid> _embeddedMetadataIds = new(StringComparer.OrdinalIgnoreCase);

    public ResourceMetadata(Guid id, Link selfLink, ResourceLinks? links = null, bool includesSelfLink = true, bool includesId = true)
    {
        Id = id;
        SelfLink = selfLink;
        Links = links ?? new ResourceLinks();
        IncludesSelfLink = includesSelfLink;
        IncludesId = includesId;
    }

    public Guid Id { get; set; }

    public Link SelfLink { get; set; }

    public ResourceLinks Links { get; set; }

    public bool IncludesSelfLink { get; }

    public bool IncludesId { get; }

    public Dictionary<string, Guid> EmbeddedMetadataIds
    {
        get => _embeddedMetadataIds;
        set => _embeddedMetadataIds = new Dictionary<string, Guid>(value ?? [], StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGetEmbeddedMetadata(object data, out ResourceMetadata metadata) =>
        _embeddedMetadata.TryGetValue(data, out metadata!);

    public void AddEmbeddedMetadata(object data, ResourceMetadata metadata, string? path = null)
    {
        _embeddedMetadata[data] = metadata;
        if (path is null)
        {
            return;
        }

        EmbeddedMetadataIds[path] = metadata.Id;
        foreach ((string nestedPath, Guid nestedId) in metadata.EmbeddedMetadataIds)
        {
            EmbeddedMetadataIds[$"{path}.{nestedPath}"] = nestedId;
        }
    }

    public ResourceMetadata GetOrAddEmbeddedMetadata(object data, string? path = null)
    {
        if (TryGetEmbeddedMetadata(data, out ResourceMetadata metadata))
        {
            return metadata;
        }

        Guid id = path is not null && EmbeddedMetadataIds.TryGetValue(path, out Guid embeddedId)
            ? embeddedId
            : Guid.NewGuid();
        metadata = new ResourceMetadata(id, Link.EmptyLink(), includesSelfLink: false);
        AddEmbeddedMetadata(data, metadata, path);
        return metadata;
    }
}
