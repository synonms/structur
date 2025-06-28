using System.Text;

namespace Synonms.Structur.Core.System;

public static class StringExtensions
{
    public static string ToCamelCase(this string text)
    {
        string[] tokens = text.Trim().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
        {
            return string.Empty;
        }

        // TODO: Support acronyms e.g. XML => xml; HTTPRequest => httpRequest; MyDVDPlayer => myDVDPlayer

        string firstToken = char.ToLowerInvariant(tokens[0][0]) + tokens[0][1..];

        StringBuilder builder = new(firstToken);

        for (int i = 1; i < tokens.Length; i++)
        {
            builder.Append(Capitalise(tokens[i]));
        }

        return builder.ToString();
    }

    public static string ToPascalCase(this string text)
    {
        string[] tokens = text.Trim().Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 0)
        {
            return string.Empty;
        }

        // TODO: Support acronyms e.g. XML => XML; HTTPRequest => HTTPRequest; myDVDPlayer => MyDVDPlayer

        string firstToken = char.ToUpperInvariant(tokens[0][0]) + tokens[0][1..];

        StringBuilder builder = new(firstToken);

        for (int i = 1; i < tokens.Length; i++)
        {
            builder.Append(Capitalise(tokens[i]));
        }

        return builder.ToString();
    }

    public static bool TryParsePositiveInt(this string text, out int result)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            result = default;
            return false;
        }

        if (int.TryParse(text, out int parsedValue) is false)
        {
            result = default;
            return false;
        }

        if (parsedValue <= 0)
        {
            result = default;
            return false;
        }
                
        result = parsedValue;
        return true;
    }

    private static string Capitalise(string token)
    {
        string lowercaseToken = token.ToLowerInvariant();
        return char.ToUpperInvariant(lowercaseToken[0]) + lowercaseToken[1..];
    }
}