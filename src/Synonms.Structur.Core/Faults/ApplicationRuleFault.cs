namespace Synonms.Structur.Core.Faults;

public class ApplicationRuleFault : ApplicationFault
{
    public ApplicationRuleFault(string detail, params object?[] arguments)
        : this(detail, new FaultSource(), arguments)
    {
    }

    public ApplicationRuleFault(string detail, FaultSource source, params object?[] arguments)
        : base(nameof(ApplicationRuleFault), "Application rule", detail, source, arguments)
    {
    }
}