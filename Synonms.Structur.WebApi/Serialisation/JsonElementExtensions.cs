using System.Text.Json;

namespace Synonms.Structur.WebApi.Serialisation;

public static class JsonElementExtensions
{
    public static bool? GetOptionalBool(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) ? propertyElement.GetBoolean() : null;

    public static DateOnly? GetOptionalDateOnly(this JsonElement jsonElement, string propertyName)
    {
        DateTime? dateTime = jsonElement.GetOptionalDateTime(propertyName);

        if (dateTime is null)
        {
            return null;
        }
        
        return DateOnly.FromDateTime(dateTime.Value);
    }

    public static DateTime? GetOptionalDateTime(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) ? propertyElement.GetDateTime() : null;

    public static decimal? GetOptionalDecimal(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) ? propertyElement.GetDecimal() : null;

    public static double? GetOptionalDouble(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) ? propertyElement.GetDouble() : null;

    public static int? GetOptionalInt(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) ? propertyElement.GetInt32() : null;

    public static string? GetOptionalString(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) ? propertyElement.GetString() : null;

    public static TimeOnly? GetOptionalTimeOnly(this JsonElement jsonElement, string propertyName)
    {
        DateTime? dateTime = jsonElement.GetOptionalDateTime(propertyName);

        if (dateTime is null)
        {
            return null;
        }
        
        return TimeOnly.FromDateTime(dateTime.Value);
    }

    public static TimeSpan? GetOptionalTimeSpan(this JsonElement jsonElement, string propertyName) =>
        jsonElement.TryGetProperty(propertyName, out JsonElement propertyElement) 
            ? TimeSpan.TryParse(propertyElement.GetString(), out TimeSpan timeSpan) ? timeSpan : null 
            : null;

    public static object? GetOptionalValue(this JsonElement jsonElement, string propertyName, Type valueType) =>
        valueType switch
        {
            _ when valueType == typeof(string) => jsonElement.GetOptionalString(propertyName),
            _ when valueType == typeof(bool) => jsonElement.GetOptionalBool(propertyName),
            _ when valueType == typeof(DateOnly) => jsonElement.GetOptionalDateOnly(propertyName),
            _ when valueType == typeof(TimeOnly) => jsonElement.GetOptionalTimeOnly(propertyName),
            _ when valueType == typeof(DateTime) => jsonElement.GetOptionalDateTime(propertyName),
            _ when valueType == typeof(TimeSpan) => jsonElement.GetOptionalTimeSpan(propertyName),
            _ when valueType == typeof(decimal) => jsonElement.GetOptionalDecimal(propertyName),
            _ when valueType == typeof(double) => jsonElement.GetOptionalDouble(propertyName),
            _ when valueType == typeof(int) => jsonElement.GetOptionalInt(propertyName),
            _ => null
        };
}