namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurDescriptorAttribute : Attribute
{
    public StructurDescriptorAttribute(string? label = null, string? placeholder = null, string? description = null, string? name = null)
    {
        Label = label;
        Placeholder = placeholder;
        Description = description;
        Name = name;
    }
    
    public string? Label { get; }
    
    public string? Placeholder { get; }

    public string? Description { get; }
    
    public string? Name { get; }
}