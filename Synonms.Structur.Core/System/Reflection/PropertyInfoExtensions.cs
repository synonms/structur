using System.Reflection;

namespace Synonms.Structur.Core.System.Reflection;

public static class PropertyInfoExtensions
{
    public static bool IsNullable(this PropertyInfo property)
    {
        NullabilityInfoContext nullabilityInfoContext = new();
        NullabilityInfo info = nullabilityInfoContext.Create(property);
        
        return info.WriteState == NullabilityState.Nullable || info.ReadState == NullabilityState.Nullable;
    }
}