namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurMaxSizeAttribute : Attribute
{
    public StructurMaxSizeAttribute(int maxSize)
    {
        MaxSize = maxSize;
    }

    public int MaxSize { get; }
}