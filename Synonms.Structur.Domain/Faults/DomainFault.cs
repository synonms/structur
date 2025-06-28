using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Domain.Faults;

public abstract class DomainFault : Fault
{
    protected DomainFault(string code, string title, string detail, FaultSource source, params object?[] arguments)
        : base(code, title, detail, source, arguments)
    {
    }
}