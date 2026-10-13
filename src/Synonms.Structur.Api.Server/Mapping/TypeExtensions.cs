using System.Reflection;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Schema.Resources;

namespace Synonms.Structur.Api.Server.Mapping;

public static class TypeExtensions
{
    public static IEnumerable<PropertyInfo> GetResourceProperties(this Type resourceType)
    {
        string[] propertiesToExclude = ["Id", "Links", "SelfLink", "SupportedVersions"];

        return resourceType.GetPublicInstanceProperties(propertiesToExclude);
    }

    public static object? GetValueObjectValue(this Type valueObjectType, object? valueObject)
    {
        if (valueObject is null) return null;

        PropertyInfo? valueObjectValuePropertyInfo = valueObjectType.GetProperty("Value", BindingFlags.Instance | BindingFlags.Public);

        if (valueObjectValuePropertyInfo is null) return null;

        return valueObjectValuePropertyInfo.GetValue(valueObject);
    }
}
