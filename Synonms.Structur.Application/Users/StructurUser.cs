namespace Synonms.Structur.Application.Users;

public class StructurUser
{
    public class UserPermission
    {
        public UserPermission(string value, Guid? tenantId)
        {
            Value = value;
            TenantId = tenantId;
        }
        
        public string Value { get; }
        
        public Guid? TenantId { get; }
    }
    
    public Guid Id { get; set; }

    public IDictionary<Guid, IEnumerable<UserPermission>> PermissionsPerProductId { get; set; } = new Dictionary<Guid, IEnumerable<UserPermission>>();
}

public sealed class NoStructurUser : StructurUser
{
    public NoStructurUser()
    {
        Id = Guid.Empty;
    }
}