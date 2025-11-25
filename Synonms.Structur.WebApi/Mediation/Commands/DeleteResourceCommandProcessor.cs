using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class DeleteResourceCommandProcessor<TAggregateRoot, TResource> : ICommandHandler<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IDomainEventFactory<TAggregateRoot, TResource> _domainEventFactory;
    private readonly IDomainEventRepository<TAggregateRoot> _domainEventRepository;
    private readonly IReadAggregateRepository<TAggregateRoot> _readAggregateRepository;
    private readonly IWriteAggregateRepository<TAggregateRoot> _writeAggregateRepository;

    public DeleteResourceCommandProcessor(IDomainEventFactory<TAggregateRoot, TResource> domainEventFactory, IDomainEventRepository<TAggregateRoot> domainEventRepository, IReadAggregateRepository<TAggregateRoot> readAggregateRepository, IWriteAggregateRepository<TAggregateRoot> writeAggregateRepository)
    {
        _domainEventFactory = domainEventFactory;
        _domainEventRepository = domainEventRepository;
        _readAggregateRepository = readAggregateRepository;
        _writeAggregateRepository = writeAggregateRepository;
    }
    
    public async Task<Result<DeleteResourceCommandResponse<TAggregateRoot>>> HandleAsync(DeleteResourceCommand<TAggregateRoot> command, CancellationToken cancellationToken)
    {
        Maybe<TAggregateRoot> findOutcome = await _readAggregateRepository.FindAsync(command.Id, cancellationToken);

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
        
        Maybe<Fault> deleteOutcome = await filterOutcome.MatchAsync(
            async aggregateRoot => 
            {
                DomainEvent<TAggregateRoot> deletedEvent = _domainEventFactory.GenerateDeletedEvent(aggregateRoot.Id);

                return await deletedEvent.ApplyAsync(aggregateRoot)
                    .BindAsync(async deletedAggregateRoot => await _domainEventRepository.CreateAsync(deletedEvent, cancellationToken)
                        .BiBindAsync(async () =>
                        {
                            await _writeAggregateRepository.UpdateAsync(deletedAggregateRoot, cancellationToken);

                            return Maybe<Fault>.None;
                        }));
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