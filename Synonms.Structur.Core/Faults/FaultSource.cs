namespace Synonms.Structur.Core.Faults;

public class FaultSource
{
    public FaultSource(string? pointer = null, string? parameter = null)
    {
        Pointer = pointer;
        Parameter = parameter;
    }

    public string? Pointer { get; }

    public string? Parameter { get; }
}