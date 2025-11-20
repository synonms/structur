namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurMaxValueAttribute : Attribute
{
    public StructurMaxValueAttribute(int maximum)
    {
        DataType = typeof(int);
        Maximum = maximum;
    }

    public StructurMaxValueAttribute(long maximum)
    {
        DataType = typeof(long);
        Maximum = maximum;
    }

    public StructurMaxValueAttribute(float maximum)
    {
        DataType = typeof(float);
        Maximum = maximum;
    }

    public StructurMaxValueAttribute(double maximum)
    {
        DataType = typeof(double);
        Maximum = maximum;
    }

    public StructurMaxValueAttribute(decimal maximum)
    {
        DataType = typeof(decimal);
        Maximum = maximum;
    }

    public Type DataType { get; }

    public object Maximum { get; }
}