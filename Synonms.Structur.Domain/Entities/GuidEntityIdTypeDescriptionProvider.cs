using System.ComponentModel;

namespace Synonms.Structur.Domain.Entities;

public class GuidEntityIdTypeDescriptionProvider : TypeDescriptionProvider
{
    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
    {
        return new GuidEntityIdTypeDescriptor(objectType);
    }
}