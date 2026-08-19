namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class StructurChildResourceAttribute : Attribute
{
    public StructurChildResourceAttribute(Type childResourceType)
    {
        ChildResourceType = childResourceType;
    }

    public Type ChildResourceType { get; }
}