using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Application.Faults;

public class ApplicationRulesFault : ApplicationFault
{
    public ApplicationRulesFault(IEnumerable<ApplicationRuleFault> faults) 
        : base(nameof(ApplicationRulesFault), "Application rules", string.Join("\r\n", faults), new FaultSource())
    {
        Faults = faults;
    }

    public IEnumerable<ApplicationRuleFault> Faults { get; }
}