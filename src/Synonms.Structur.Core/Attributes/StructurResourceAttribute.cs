namespace Synonms.Structur.Core.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class StructurResourceAttribute : Attribute
{
    public StructurResourceAttribute(
        Type resourceType,
        string collectionPath,
        bool allowAnonymous = false,
        int pageLimit = 0,
        bool isCreateDisabled = false,
        bool isUpdateDisabled = false,
        bool isDeleteDisabled = false,
        string? readPolicy = null,
        string? createPolicy = null,
        string? updatePolicy = null,
        string? deletePolicy = null,
        bool isDefaultRepresentation = false,
        bool isReadDisabled = false,
        bool isCreateFormDisabled = false,
        bool isEditFormDisabled = false)
    {
        ResourceType = resourceType;
        CollectionPath = collectionPath;
        AllowAnonymous = allowAnonymous;
        PageLimit = Math.Clamp(pageLimit, 0, int.MaxValue);
        IsCreateDisabled = isCreateDisabled;
        IsUpdateDisabled = isUpdateDisabled;
        IsDeleteDisabled = isDeleteDisabled;
        ReadPolicy = readPolicy;
        CreatePolicy = createPolicy;
        UpdatePolicy = updatePolicy;
        DeletePolicy = deletePolicy;
        IsDefaultRepresentation = isDefaultRepresentation;
        IsReadDisabled = isReadDisabled;
        IsCreateFormDisabled = isCreateFormDisabled;
        IsEditFormDisabled = isEditFormDisabled;
    }

    public Type ResourceType { get; }

    public string CollectionPath { get; }

    public bool AllowAnonymous { get; }

    public int PageLimit { get; }

    public bool IsCreateDisabled { get; }

    public bool IsUpdateDisabled { get; }

    public bool IsDeleteDisabled { get; }

    public string? ReadPolicy { get; }

    public string? CreatePolicy { get; }

    public string? UpdatePolicy { get; }

    public string? DeletePolicy { get; }

    public string[] SupportedApiVersions { get; set; } = [];

    public bool IsDefaultRepresentation { get; }

    public bool IsReadDisabled { get; }

    public bool IsCreateFormDisabled { get; }

    public bool IsEditFormDisabled { get; }
}
