using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class UpdateResourceCommandProcessor<TAggregateRoot, TResource> : ICommandHandler<UpdateResourceCommand<TAggregateRoot, TResource>, UpdateResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IDomainEventFactory<TAggregateRoot, TResource> _domainEventFactory;
    private readonly IDomainEventRepository<TAggregateRoot> _domainEventRepository;
    private readonly IReadAggregateRepository<TAggregateRoot> _readAggregateRepository;
    private readonly IWriteAggregateRepository<TAggregateRoot> _writeAggregateRepository;

    public UpdateResourceCommandProcessor(IDomainEventFactory<TAggregateRoot, TResource> domainEventFactory, IDomainEventRepository<TAggregateRoot> domainEventRepository, IReadAggregateRepository<TAggregateRoot> readAggregateRepository, IWriteAggregateRepository<TAggregateRoot> writeAggregateRepository)
    {
        _domainEventFactory = domainEventFactory;
        _domainEventRepository = domainEventRepository;
        _readAggregateRepository = readAggregateRepository;
        _writeAggregateRepository = writeAggregateRepository;
    }

    public async Task<Result<UpdateResourceCommandResponse<TAggregateRoot>>> HandleAsync(UpdateResourceCommand<TAggregateRoot, TResource> command, CancellationToken cancellationToken)
    {
        Result<TAggregateRoot> findOutcome = await _readAggregateRepository.FindAsync(command.Id, cancellationToken)
            .MatchAsync(
                Result<TAggregateRoot>.SuccessAsync,
                () =>
                {
                    EntityNotFoundFault fault = new("{0} with id '{1}' not found.", nameof(TAggregateRoot), command.Id);
                    return Result<TAggregateRoot>.FailureAsync(fault);
                });

        Result<TAggregateRoot> editOutcome = await findOutcome
            .BindAsync(async aggregateRoot =>
            {
                DomainEvent<TAggregateRoot> updatedEvent = _domainEventFactory.GenerateUpdatedEvent(command.Resource);

                return await updatedEvent.ApplyAsync(aggregateRoot)
                    .BindAsync(async updatedAggregateRoot => await _domainEventRepository.CreateAsync(updatedEvent, cancellationToken) 
                        .ToResultAsync(async () =>
                        {
                            await _writeAggregateRepository.UpdateAsync(updatedAggregateRoot, cancellationToken);

                            return Result<TAggregateRoot>.Success(updatedAggregateRoot);
                        }));
            });

        return editOutcome.Bind(aggregateRoot =>
        {
            UpdateResourceCommandResponse<TAggregateRoot> response = new(aggregateRoot);
            return Result<UpdateResourceCommandResponse<TAggregateRoot>>.Success(response);
        });;
    }
}