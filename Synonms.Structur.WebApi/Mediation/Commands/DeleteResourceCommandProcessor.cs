using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class DeleteResourceCommandProcessor<TAggregateRoot> : ICommandHandler<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IAggregateRepository<TAggregateRoot> _aggregateRepository;

    public DeleteResourceCommandProcessor(IAggregateRepository<TAggregateRoot> aggregateRepository)
    {
        _aggregateRepository = aggregateRepository;
    }
    
    public async Task<Result<DeleteResourceCommandResponse<TAggregateRoot>>> HandleAsync(DeleteResourceCommand<TAggregateRoot> command, CancellationToken cancellationToken)
    {
        Maybe<TAggregateRoot> findOutcome = await _aggregateRepository.FindAsync(command.Id, cancellationToken);

        Maybe<TAggregateRoot> filterOutcome = findOutcome.Bind(aggregateRoot =>
        {
            if (command.Filter is null)
            {
                return aggregateRoot;
            }

            if (command.Filter(aggregateRoot))
            {
                return aggregateRoot;
            }
            
            return Maybe<TAggregateRoot>.None;
        });

        Maybe<Fault> deleteOutcome = await filterOutcome
            .MatchAsync(
                async aggregateRoot => 
                {
                    await _aggregateRepository.DeleteAsync(aggregateRoot.Id, cancellationToken);
                    await _aggregateRepository.SaveChangesAsync(cancellationToken);

                    return Maybe<Fault>.None;
                },
                () =>
                {
                    EntityNotFoundFault fault = new ("{0} with id '{1}' not found.", nameof(TAggregateRoot), command.Id);

                    return Maybe<Fault>.SomeAsync(fault);
                });

        DeleteResourceCommandResponse<TAggregateRoot> response = new ();
        
        return response;
    }
}