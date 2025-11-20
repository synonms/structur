using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Mapping;

public interface IChildResourceMapper
{
    object? Map(object value);
}

public interface IChildResourceMapper<in TAggregateMember, out TChildResource> : IChildResourceMapper
    where TAggregateMember : AggregateMember<TAggregateMember>
    where TChildResource : ChildResource
{
    TChildResource? Map(TAggregateMember aggregateMember);
}