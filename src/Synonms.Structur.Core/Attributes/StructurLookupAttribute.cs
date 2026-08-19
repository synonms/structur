namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class StructurLookupAttribute : Attribute
{
    public StructurLookupAttribute(string discriminator)
    {
        Discriminator = discriminator;
    }
    
    public string Discriminator { get; }
}