using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mapping;

public interface IChildResourceMapper
{
    object? Map(object value);

    ResourceEnvelope<object>? MapEnvelope(object value);
}

public interface IChildResourceMapper<TAggregateMember, TChildResource> : IChildResourceMapper
    where TAggregateMember : AggregateMember<TAggregateMember>
{
    TChildResource? Map(TAggregateMember aggregateMember);

    ResourceEnvelope<TChildResource>? MapEnvelope(TAggregateMember aggregateMember) =>
        new(Map(aggregateMember)!, new ResourceMetadata(aggregateMember.Id.Value, Link.EmptyLink(), includesSelfLink: false));

    ResourceEnvelope<object>? IChildResourceMapper.MapEnvelope(object value)
    {
        if (value is not TAggregateMember aggregateMember || MapEnvelope(aggregateMember) is not { } envelope)
        {
            return null;
        }

        return new ResourceEnvelope<object>(envelope.Data!, envelope.Metadata);
    }
}
