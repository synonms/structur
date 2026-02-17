namespace Synonms.Structur.Core.Entities;

public static class TypeExtensions
{
    public static bool IsEntityId(this Type type) =>
        type is
        {
            IsInterface: false, 
            IsAbstract: false, 
            IsGenericType: true
        }
        && type.GetGenericTypeDefinition() == typeof(EntityId<>);
}