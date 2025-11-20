using Synonms.Structur.Application.Mapping;
using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class UpdateResourceCommandProcessor<TAggregateRoot, TResource> : ICommandHandler<UpdateResourceCommand<TAggregateRoot, TResource>, UpdateResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IAggregateRepository<TAggregateRoot> _aggregateRepository;
    private readonly IAggregateUpdater<TAggregateRoot, TResource> _aggregateUpdater;

    public UpdateResourceCommandProcessor(IAggregateRepository<TAggregateRoot> aggregateRepository, IAggregateUpdater<TAggregateRoot, TResource> aggregateUpdater)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateUpdater = aggregateUpdater;
    }

    public async Task<Result<UpdateResourceCommandResponse<TAggregateRoot>>> HandleAsync(UpdateResourceCommand<TAggregateRoot, TResource> command, CancellationToken cancellationToken)
    {
        Result<TAggregateRoot> findOutcome = await _aggregateRepository.FindAsync(command.Id, cancellationToken)
            .MatchAsync(
                Result<TAggregateRoot>.SuccessAsync,
                () =>
                {
                    EntityNotFoundFault fault = new("{0} with id '{1}' not found.", nameof(TAggregateRoot), command.Id);
                    return Result<TAggregateRoot>.FailureAsync(fault);
                });

        Result<TAggregateRoot> editOutcome = await findOutcome
            .BindAsync(async aggregateRoot => (await _aggregateUpdater.UpdateAsync(aggregateRoot, command.Resource, cancellationToken)).ToResult(() => aggregateRoot));

        Result<TAggregateRoot> persistOutcome = await editOutcome
            .BindAsync(async aggregateRoot =>
            {
                await _aggregateRepository.UpdateAsync(aggregateRoot, cancellationToken);
                await _aggregateRepository.SaveChangesAsync(cancellationToken);
                
                return Result<TAggregateRoot>.Success(aggregateRoot);
            });

        return persistOutcome.Bind(aggregateRoot =>
        {
            UpdateResourceCommandResponse<TAggregateRoot> response = new(aggregateRoot);
            return Result<UpdateResourceCommandResponse<TAggregateRoot>>.Success(response);
        });;
    }
}