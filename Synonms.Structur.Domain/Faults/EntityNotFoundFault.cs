using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Domain.Faults;

public class EntityNotFoundFault : DomainFault
{
    public EntityNotFoundFault(string detail, params object?[] arguments)
        : this(detail, new FaultSource(), arguments)
    {
    }

    public EntityNotFoundFault(string detail, FaultSource source, params object?[] arguments)
        : base(nameof(EntityNotFoundFault), "Entity not found", detail, source, arguments)
    {
    }
}