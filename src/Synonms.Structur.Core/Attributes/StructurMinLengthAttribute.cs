namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurMinLengthAttribute : Attribute
{
    public StructurMinLengthAttribute(int minLength)
    {
        MinLength = minLength;
    }

    public int MinLength { get; }
}