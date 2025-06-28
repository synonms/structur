namespace Synonms.Structur.Core.Faults;

public sealed class AggregateFault : Fault
{
    public AggregateFault(IEnumerable<Fault> faults) 
        : base(nameof(AggregateFault), "Aggregate errors", string.Join("\r\n", faults), new FaultSource())
    {
        Faults = faults;
    }

    public IEnumerable<Fault> Faults { get; }
}