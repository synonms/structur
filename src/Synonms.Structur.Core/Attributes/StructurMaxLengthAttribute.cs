namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurMaxLengthAttribute : Attribute
{
    public StructurMaxLengthAttribute(int maxLength)
    {
        MaxLength = maxLength;
    }

    public int MaxLength { get; }
}