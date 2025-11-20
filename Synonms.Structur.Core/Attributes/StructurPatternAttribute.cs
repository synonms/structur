namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurPatternAttribute : Attribute
{
    public string Pattern { get; }

    public StructurPatternAttribute(string pattern)
    {
        Pattern = pattern;
    }
}