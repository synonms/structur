namespace Synonms.Structur.Core.Faults;

public class MediationFault : Fault
{
    public MediationFault(string detail, params object?[] arguments)
        : this(detail, new FaultSource(), arguments)
    {
    }

    public MediationFault(string detail, FaultSource source, params object?[] arguments)
        : base(nameof(MediationFault), "Mediation", detail, source, arguments)
    {
    }
}