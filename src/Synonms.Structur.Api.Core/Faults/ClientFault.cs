using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Api.Core.Faults;

public class ClientFault : Fault
{
    public ClientFault(string detail, params object?[] arguments) 
        : this(detail, new FaultSource(), arguments)
    {
    }

    public ClientFault(string detail, FaultSource source, params object?[] arguments) 
        : base(nameof(ClientFault), "Client Fault", detail, source, arguments)
    {
    }
}