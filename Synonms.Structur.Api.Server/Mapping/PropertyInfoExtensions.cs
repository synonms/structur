using System.Reflection;

namespace Synonms.Structur.Api.Server.Mapping;

public static class PropertyInfoExtensions
{
    public static PropertyInfo? GetMatchingPropertyFrom<T>(this PropertyInfo resourcePropertyInfo) =>
        typeof(T).GetProperty(resourcePropertyInfo.Name, BindingFlags.Instance | BindingFlags.Public);
}