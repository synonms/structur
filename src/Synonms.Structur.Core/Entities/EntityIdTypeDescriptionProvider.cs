using System.ComponentModel;

namespace Synonms.Structur.Core.Entities;

public class EntityIdTypeDescriptionProvider : TypeDescriptionProvider
{
    public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object? instance)
    {
        return new EntityIdTypeDescriptor(objectType);
    }
}