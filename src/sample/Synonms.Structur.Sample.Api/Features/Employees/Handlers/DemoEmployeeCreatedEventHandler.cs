using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Sample.Api.Features.Employees.Events;

namespace Synonms.Structur.Sample.Api.Features.Employees.Handlers;

public class DemoEmployeeCreatedEventHandler(ILogger<DemoEmployeeCreatedEventHandler> logger) : DomainEventHandler<EmployeeCreatedEvent>
{
    public override Task<Maybe<Fault>> HandleAsync(EmployeeCreatedEvent updatedEvent, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("{CkassName}.{MethodName}: Handling event", nameof(DemoEmployeeCreatedEventHandler), nameof(HandleAsync));
        
        bool isRandomSituation = Random.Shared.Next(0, 2) == 0;

        if (isRandomSituation)
        {
            logger.LogInformation("{CkassName}.{MethodName}: Simulating random situation", nameof(DemoEmployeeCreatedEventHandler), nameof(HandleAsync));
        }
        else
        {
            logger.LogInformation("{CkassName}.{MethodName}: Simulating success", nameof(DemoEmployeeCreatedEventHandler), nameof(HandleAsync));
        }
        
        return Maybe<Fault>.NoneAsync;
    }
}