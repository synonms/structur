namespace Synonms.Structur.Core.Versioning;

public static class VersionExtensions
{
    public static bool IsUnspecified(this Version version)
    {
        return version is { Major: <= 0, Minor: <= 0, Build: <= 0, Revision: <= 0 };
    }

    public static string ToMinifiedString(this Version version, bool isPrefixed = false)
    {
        string output = string.Empty;
        bool isIncrementDetected = false;

        if (version.Revision > 0)
        {
            isIncrementDetected = true;
            output = $".{version.Revision}";
        }

        if (isIncrementDetected || version.Build > 0)
        {
            isIncrementDetected = true;
            output = $".{version.Build}{output}";
        }

        if (isIncrementDetected || version.Minor > 0)
        {
            output = $".{version.Minor}{output}";
        }

        output = $"{version.Major}{output}";

        return isPrefixed ? $"v{output}" : output;
    }
}