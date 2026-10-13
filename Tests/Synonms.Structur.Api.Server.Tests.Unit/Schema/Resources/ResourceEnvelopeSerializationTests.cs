using System.Text.Json;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Api.Core.Serialisation.Ion;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Hypermedia;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Synonms.Structur.Api.Server.Tests.Unit.Schema.Resources;

public class ResourceEnvelopeSerializationTests
{
    [Fact]
    public void DefaultDocument_UsesEnvelopeIdInsteadOfDataIdAndDoesNotSerializeDataLinks()
    {
        Guid envelopeId = Guid.NewGuid();
        Contract data = new()
        {
            Id = Guid.NewGuid(),
            Links = new Dictionary<string, string> { ["forged"] = "link" },
            Name = "plain",
            Child = new NestedContract { Id = Guid.NewGuid(), Links = new Dictionary<string, string> { ["forged"] = "nested" }, Name = "nested" }
        };
        Guid childId = Guid.NewGuid();
        ResourceLinks links = new() { ["related"] = Link.RelationLink(new Uri("/related", UriKind.Relative)) };
        ResourceMetadata metadata = new(envelopeId, Link.SelfLink(new Uri("/items/" + envelopeId, UriKind.Relative)), links);
        metadata.AddEmbeddedMetadata(data.Child, new ResourceMetadata(childId, Link.EmptyLink(), includesSelfLink: false));
        ResourceDocument<Contract> document = new(new ResourceEnvelope<Contract>(data, metadata));
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new DefaultCustomJsonConverterFactory(new Version(1, 0)), new DefaultLinkJsonConverter() }
        };

        using JsonDocument result = JsonDocument.Parse(JsonSerializer.Serialize(document, options));
        JsonElement value = result.RootElement.GetProperty("value");

        Assert.Equal(envelopeId, value.GetProperty("id").GetGuid());
        Assert.Equal("plain", value.GetProperty("name").GetString());
        Assert.False(value.TryGetProperty("links", out _));
        Assert.False(value.TryGetProperty("self", out _));
        Assert.Equal(childId, value.GetProperty("child").GetProperty("id").GetGuid());
        Assert.False(value.GetProperty("child").TryGetProperty("links", out _));

        ResourceDocument<Contract>? restored = JsonSerializer.Deserialize<ResourceDocument<Contract>>(result.RootElement.GetRawText(), options);
        Assert.NotNull(restored);
        Assert.Equal(envelopeId, restored.Metadata.Id);
        Assert.Equal(Guid.Empty, restored.Resource.Id);
        Assert.NotNull(restored.Resource.Child);
        Assert.Equal(Guid.Empty, restored.Resource.Child.Id);
        Assert.True(restored.Metadata.TryGetEmbeddedMetadata(restored.Resource.Child, out ResourceMetadata restoredChildMetadata));
        Assert.Equal(childId, restoredChildMetadata.Id);
    }

    [Fact]
    public void IonDocument_UsesEnvelopeMetadataAndOmitsDataOwnedMetadataProperties()
    {
        Guid envelopeId = Guid.NewGuid();
        Contract data = new()
        {
            Id = Guid.NewGuid(),
            Links = new Dictionary<string, string> { ["forged"] = "link" },
            Name = "plain"
        };
        ResourceMetadata metadata = new(
            envelopeId,
            Link.SelfLink(new Uri("/items/" + envelopeId, UriKind.Relative)),
            new ResourceLinks { ["related"] = Link.RelationLink(new Uri("/related", UriKind.Relative)) });
        ResourceDocument<Contract> document = new(new ResourceEnvelope<Contract>(data, metadata));
        JsonSerializerOptions options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new IonCustomJsonConverterFactory(new Version(1, 0)), new IonLinkJsonConverter() }
        };

        using JsonDocument result = JsonDocument.Parse(JsonSerializer.Serialize(document, options));
        JsonElement value = result.RootElement.GetProperty("value");

        Assert.Equal(envelopeId, value.GetProperty("id").GetGuid());
        Assert.Equal("plain", value.GetProperty("name").GetString());
        Assert.True(value.TryGetProperty("self", out _));
        Assert.True(value.TryGetProperty("related", out _));
        Assert.False(value.TryGetProperty("links", out _));
    }

    [Fact]
    public void InputReader_TreatsIdAsEnvelopeMetadataAndDiscardsUntrustedLinks()
    {
        Guid id = Guid.NewGuid();
        Guid childId = Guid.NewGuid();
        DefaultHttpContext context = new();
        JsonSerializerOptions options = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        const string bodyTemplate = """{"id":"ROOT_ID","name":"plain","links":{"admin":{"uri":"/admin","relation":"related","method":"GET"}},"child":{"id":"CHILD_ID","name":"nested","links":{"forged":true}}}""";

        Contract? contract = (Contract?)ResourceContractInputReader.Deserialize(
            bodyTemplate.Replace("ROOT_ID", id.ToString()).Replace("CHILD_ID", childId.ToString()),
            typeof(Contract),
            options,
            context);
        ResourceMetadata? metadata = context.Items[ResourceContractInputReader.MetadataItemKey] as ResourceMetadata;

        Assert.NotNull(contract);
        Assert.NotNull(metadata);
        Assert.Equal(id, metadata.Id);
        Assert.Equal(Guid.Empty, contract.Id);
        Assert.Empty(contract.Links);
        Assert.NotNull(contract.Child);
        Assert.True(metadata.TryGetEmbeddedMetadata(contract.Child, out ResourceMetadata nestedMetadata));
        Assert.Equal(childId, nestedMetadata.Id);
        Assert.Equal(childId, metadata.EmbeddedMetadataIds["child"]);
        Assert.Equal(Guid.Empty, contract.Child.Id);
        Assert.Empty(contract.Child.Links);
    }

    private sealed class Contract
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> Links { get; set; } = [];
        public string Name { get; set; } = string.Empty;
        public NestedContract? Child { get; set; }
    }

    private sealed class NestedContract
    {
        public Guid Id { get; set; }
        public Dictionary<string, string> Links { get; set; } = [];
        public string Name { get; set; } = string.Empty;
    }
}
