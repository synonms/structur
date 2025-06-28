using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Domain.Faults;

public class DomainRuleFault : DomainFault
{
    public DomainRuleFault(string detail, params object?[] arguments)
        : this(detail, new FaultSource(), arguments)
    {
    }

    public DomainRuleFault(string detail, FaultSource source, params object?[] arguments)
        : base(nameof(DomainRuleFault), "Domain rule", detail, source, arguments)
    {
    }
}