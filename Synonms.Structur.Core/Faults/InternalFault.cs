namespace Synonms.Structur.Core.Faults;

public class InternalFault : Fault
{
    public InternalFault(string detail, params object?[] arguments)
        : this(detail, new FaultSource(), arguments)
    {
    }

    public InternalFault(string detail, FaultSource source, params object?[] arguments)
        : base(nameof(InternalFault), "Unexpected error", detail, source, arguments)
    {
    }
}