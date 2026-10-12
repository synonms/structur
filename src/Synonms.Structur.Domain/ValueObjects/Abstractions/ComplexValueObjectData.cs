namespace Synonms.Structur.Domain.ValueObjects.Abstractions;

/// <summary>Explicit required-member policy for trusted reconstruction. Empty strings remain representable.</summary>
public static class ComplexValueObjectData
{
    public static T Required<T>(T? value, string memberName) where T : class =>
        value ?? throw new ArgumentException($"Required complex value-object member '{memberName}' is missing or null.");

    public static T Required<T>(T? value, string memberName) where T : struct =>
        value ?? throw new ArgumentException($"Required complex value-object member '{memberName}' is missing or null.");
}
