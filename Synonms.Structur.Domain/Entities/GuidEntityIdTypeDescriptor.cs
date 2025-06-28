using System.ComponentModel;

namespace Synonms.Structur.Domain.Entities;

public class GuidEntityIdTypeDescriptor : CustomTypeDescriptor
{
    private readonly Type _objectType;

    public GuidEntityIdTypeDescriptor(Type objectType)
    {
        _objectType = objectType;
    }

    public override TypeConverter GetConverter()
    {
        Type genericArg = _objectType.GenericTypeArguments[0];
        Type converterType = typeof(GuidEntityIdTypeConverter<>).MakeGenericType(genericArg);

        if (Activator.CreateInstance(converterType) is not TypeConverter converter)
        {
            throw new NullReferenceException($"Unable to instantiate converter type [{converterType}].");
        }

        return converter;
    }
}