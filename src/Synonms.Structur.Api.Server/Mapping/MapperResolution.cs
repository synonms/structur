namespace Synonms.Structur.Api.Server.Mapping;

internal static class MapperResolution
{
    internal static TMapper? Resolve<TMapper>(IEnumerable<TMapper> registrations, Type contract, Type defaultMapperDefinition, Type sourceType, Type destinationType)
        where TMapper : class
    {
        Type[] matchingTypes = registrations
            .Select(mapper => mapper.GetType())
            .Distinct()
            .Where(type => type.GetInterfaces().Contains(contract))
            .ToArray();
        Type[] customTypes = matchingTypes.Where(type =>
            !type.IsGenericType || type.GetGenericTypeDefinition() != defaultMapperDefinition).ToArray();

        if (customTypes.Length > 1)
        {
            throw new InvalidOperationException(
                $"Multiple custom mappers are registered for {sourceType.FullName} -> {destinationType.FullName}: {string.Join(", ", customTypes.Select(type => type.FullName))}.");
        }

        Type? selectedType = customTypes.SingleOrDefault() ?? matchingTypes.SingleOrDefault();
        if (selectedType is null)
        {
            return null;
        }

        return registrations.First(mapper => mapper.GetType() == selectedType);
    }
}
