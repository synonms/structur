namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class StructurResourceAttribute : Attribute
{
    public StructurResourceAttribute(Type resourceType, string collectionPath, bool allowAnonymous = false, int pageLimit = 0, bool isCreateDisabled = false, bool isUpdateDisabled = false, bool isDeleteDisabled = false)
    {
        ResourceType = resourceType;
        CollectionPath = collectionPath;
        AllowAnonymous = allowAnonymous;
        PageLimit = Math.Clamp(pageLimit, 0, int.MaxValue);
        IsCreateDisabled = isCreateDisabled;
        IsUpdateDisabled = isUpdateDisabled;
        IsDeleteDisabled = isDeleteDisabled;
    }

    public Type ResourceType { get; }
    
    public string CollectionPath { get; }
    
    public bool AllowAnonymous { get; }

    public int PageLimit { get; }

    public bool IsCreateDisabled { get; }
    
    public bool IsUpdateDisabled { get; }
    
    public bool IsDeleteDisabled { get; }
}