namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class StructurOptionAttribute : Attribute
{
    public StructurOptionAttribute(object id, string? label, bool isEnabled = true)
    {
        Id = id;
        Label = label;
        IsEnabled = isEnabled;
    }
    
    public object Id { get; }
    
    public string? Label { get; }
    
    public bool IsEnabled { get; }
}