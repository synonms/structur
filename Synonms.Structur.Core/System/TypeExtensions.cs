using System.Collections;

namespace Synonms.Structur.Core.System;

public static class TypeExtensions
{
    public static Type? GetArrayOrEnumerableElementType(this Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        return type.IsEnumerable() ? type.GetGenericArguments().FirstOrDefault() : null;
    }

    public static bool IsArrayOrEnumerable(this Type type) =>
        type.IsArray || type.IsEnumerable();

    public static bool IsEnumerable(this Type type) =>
        type.IsGenericType && type.GetInterfaces().Any(x => x == typeof(IEnumerable));
}