using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Domain.Faults;

public class DomainRulesFault : DomainFault
{
    public DomainRulesFault(IEnumerable<DomainRuleFault> faults) 
        : base(nameof(DomainRulesFault), "Domain rules", string.Join("\r\n", faults), new FaultSource())
    {
        Faults = faults;
    }

    public IEnumerable<DomainRuleFault> Faults { get; }
}