using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.Domain.Events;

public static class DomainEventFaults
{
    private const string CannotApplyToNonNullTemplate = "Unable to apply {0} to non null {1}.";
    private const string CannotApplyToNullTemplate = "Unable to apply {0} to null {1}.";
    private const string InvalidDataTypeTemplate = "Unable to interpret data object from {0} as {1}.";

    public static DomainEventFault CannotApplyToNonNull(string domainEventType, string aggregateType) =>
        new(CannotApplyToNonNullTemplate, domainEventType, aggregateType);

    public static DomainEventFault CannotApplyToNull(string domainEventType, string aggregateType) =>
        new(CannotApplyToNullTemplate, domainEventType, aggregateType);
    
    public static DomainEventFault InvalidDataType(string domainEventType, string domainEventDataType) =>
        new(InvalidDataTypeTemplate, domainEventType, domainEventDataType);
}