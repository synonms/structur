using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Employees.Events;

namespace Synonms.Structur.Sample.Api.Features.Employees.Handlers;

public class UnstableEmployeeCreatedEventHandler(ILogger<UnstableEmployeeCreatedEventHandler> logger) : DomainEventHandler<EmployeeCreatedEvent>
{
    public override Task<Maybe<Fault>> HandleAsync(EmployeeCreatedEvent updatedEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{CkassName}.{MethodName}: Handling event", nameof(UnstableEmployeeCreatedEventHandler), nameof(HandleAsync));
        
        bool isRandomFailure = Random.Shared.Next(0, 2) == 0;

        if (isRandomFailure)
        {
            logger.LogInformation("{CkassName}.{MethodName}: Simulating failure", nameof(UnstableEmployeeCreatedEventHandler), nameof(HandleAsync));
        }
        else
        {
            logger.LogInformation("{CkassName}.{MethodName}: Simulating success", nameof(UnstableEmployeeCreatedEventHandler), nameof(HandleAsync));
        }
        
        return isRandomFailure 
            ? Maybe<Fault>.SomeAsync(new Fault("SAMPLE01", "Simulated fault", "A simulated fault returned by a domain event handler.", new FaultSource())) 
            : Maybe<Fault>.NoneAsync;
    }
}