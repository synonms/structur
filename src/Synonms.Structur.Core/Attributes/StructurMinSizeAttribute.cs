namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurMinSizeAttribute : Attribute
{
    public StructurMinSizeAttribute(int minSize)
    {
        MinSize = minSize;
    }

    public int MinSize { get; }
}