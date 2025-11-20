namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurMinValueAttribute : Attribute
{
    public StructurMinValueAttribute(int minimum)
    {
        DataType = typeof(int);
        Minimum = minimum;
    }

    public StructurMinValueAttribute(long minimum)
    {
        DataType = typeof(long);
        Minimum = minimum;
    }

    public StructurMinValueAttribute(float minimum)
    {
        DataType = typeof(float);
        Minimum = minimum;
    }

    public StructurMinValueAttribute(double minimum)
    {
        DataType = typeof(double);
        Minimum = minimum;
    }

    public StructurMinValueAttribute(decimal minimum)
    {
        DataType = typeof(decimal);
        Minimum = minimum;
    }
    
    public Type DataType { get; }
    
    public object Minimum { get; }
}