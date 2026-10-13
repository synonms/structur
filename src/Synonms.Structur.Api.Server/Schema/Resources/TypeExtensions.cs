using System.Reflection;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Core.System;

namespace Synonms.Structur.Api.Server.Schema.Resources;

public static class TypeExtensions
{
    public static IEnumerable<PropertyInfo> GetPublicInstanceProperties(this Type type) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

    public static IEnumerable<PropertyInfo> GetPublicInstanceProperties(this Type type, string[] excludePropertyNames) =>
        type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(propertyInfo => excludePropertyNames.Contains(propertyInfo.Name) is false);

}
