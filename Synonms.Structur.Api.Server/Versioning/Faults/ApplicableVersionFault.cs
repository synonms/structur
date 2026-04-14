using Synonms.Structur.Core.Faults;

namespace Synonms.Structur.Api.Server.Versioning.Faults;

public class ApplicableVersionFault : ApplicationFault
{
    public ApplicableVersionFault(Type resourceType)
        : base(nameof(ApplicableVersionFault), "Applicable Version", "Unable to determine which version is applicable for resource '{ResourceType}'.", new FaultSource(), resourceType.Name)
    {
    }
}