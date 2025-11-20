using System.ComponentModel;

namespace Synonms.Structur.Domain.Entities;

public class EntityIdTypeDescriptionProvider : TypeDescriptionProvider
{
    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
    {
        return new EntityIdTypeDescriptor(objectType);
    }
}