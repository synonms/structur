using System.Reflection;

namespace Synonms.Structur.Api.Core.Schema.Resources;

/// <summary>Creates plain resource DTOs, including positional records with public constructors.</summary>
public static class ResourceContractActivator
{
    public static TResource Create<TResource>() =>
        (TResource)Create(typeof(TResource));

    public static object Create(Type contractType)
    {
        ArgumentNullException.ThrowIfNull(contractType);

        ConstructorInfo? parameterlessConstructor = contractType.GetConstructor(Type.EmptyTypes);
        if (parameterlessConstructor is not null)
        {
            return parameterlessConstructor.Invoke(null);
        }

        ConstructorInfo? constructor = contractType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .OrderByDescending(candidate => candidate.GetParameters().Length)
            .FirstOrDefault(candidate => candidate.GetParameters().All(parameter =>
            {
                PropertyInfo? property = contractType.GetProperty(parameter.Name!, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                return property is not null && property.PropertyType == parameter.ParameterType;
            }));

        if (constructor is null)
        {
            throw new InvalidOperationException(
                $"Plain resource contract {contractType.FullName} must have a public parameterless constructor or a public constructor whose parameters correspond to public properties.");
        }

        object?[] arguments = constructor.GetParameters()
            .Select(parameter => parameter.HasDefaultValue ? parameter.DefaultValue : GetDefaultValue(parameter.ParameterType))
            .ToArray();

        return constructor.Invoke(arguments);
    }

    private static object? GetDefaultValue(Type type) =>
        type.IsValueType ? Activator.CreateInstance(type) : null;
}
