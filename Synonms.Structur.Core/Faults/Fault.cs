using System.Globalization;

namespace Synonms.Structur.Core.Faults;

public class Fault
{
    public Fault(string code, string title, string detail, FaultSource source, params object?[] arguments)
    {
        Id = Guid.NewGuid();
        Code = code;
        Title = title;
        Detail = detail;
        Source = source;
        Arguments = arguments;
    }

    public Guid Id { get; }

    public string Code { get; }

    public string Title { get; }

    public string Detail { get; }

    public FaultSource Source { get; }
        
    public object?[] Arguments { get; }

    public override string ToString()
    {
        return $"[{Code}] {Title}: {string.Format(CultureInfo.InvariantCulture, Detail, Arguments)}";
    }
}