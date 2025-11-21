namespace Synonms.Structur.WebApi.Serialisation;

public static class StringExtensions
{
    public static Type? GetFormFieldValueType(this string? type) => type switch
    {
        DataTypes.Array => typeof(IEnumerable<>),
        DataTypes.Boolean => typeof(bool),
        DataTypes.DateOnly => typeof(DateOnly),
        DataTypes.DateTime => typeof(DateTime),
        DataTypes.Duration =>  typeof(TimeSpan),
        DataTypes.Decimal => typeof(decimal),
        DataTypes.Integer =>typeof(int),
        DataTypes.Number => typeof(double),
        DataTypes.Object => typeof(object),
        DataTypes.String => typeof(string),
        DataTypes.TimeOnly => typeof(TimeOnly?),
        _ => null
    };
}