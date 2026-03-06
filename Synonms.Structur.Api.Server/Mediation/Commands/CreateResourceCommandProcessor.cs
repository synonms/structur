using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Events;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Events;

namespace Synonms.Structur.Api.Server.Mediation.Commands;

public class CreateResourceCommandProcessor<TAggregateRoot, TResource> : ICommandHandler<CreateResourceCommand<TAggregateRoot, TResource>, CreateResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IDomainEventFactory<TAggregateRoot, TResource> _domainEventFactory;
    private readonly IDomainEventRepository<TAggregateRoot> _domainEventRepository;
    private readonly IReadAggregateRepository<TAggregateRoot> _readAggregateRepository;
    private readonly IWriteAggregateRepository<TAggregateRoot> _writeAggregateRepository;

    public CreateResourceCommandProcessor(IDomainEventFactory<TAggregateRoot, TResource> domainEventFactory, IDomainEventRepository<TAggregateRoot> domainEventRepository, IReadAggregateRepository<TAggregateRoot> readAggregateRepository, IWriteAggregateRepository<TAggregateRoot> writeAggregateRepository)
    {
        _domainEventFactory = domainEventFactory;
        _domainEventRepository = domainEventRepository;
        _readAggregateRepository = readAggregateRepository;
        _writeAggregateRepository = writeAggregateRepository;
    }

    public async Task<Result<CreateResourceCommandResponse<TAggregateRoot>>> HandleAsync(CreateResourceCommand<TAggregateRoot, TResource> command, CancellationToken cancellationToken)
    {
        Maybe<TAggregateRoot> existingOutcome = await _readAggregateRepository.FindAsync((EntityId<TAggregateRoot>)command.Resource.Id, cancellationToken);

        Result<DomainEvent<TAggregateRoot>> generateEventOutcome = await existingOutcome
            .MatchAsync(
                existingAggregate => Result<DomainEvent<TAggregateRoot>>.FailureAsync(new DomainRuleFault("{entityType} Id '{id}' already exists.", nameof(TAggregateRoot), command.Resource.Id)), 
                async () => await _domainEventFactory.GenerateCreatedEvent(command.Resource, cancellationToken));
        
        Result<TAggregateRoot> createOutcome = await generateEventOutcome
            .BindAsync(async createdEvent => 
                await createdEvent.ApplyAsync(null)
                    .BindAsync(async aggregateRoot => await _domainEventRepository.CreateAsync(createdEvent, cancellationToken)
                        .ToResultAsync(async () =>
                        {
                            await _writeAggregateRepository.AddAsync(aggregateRoot, cancellationToken);

                            return Result<TAggregateRoot>.Success(aggregateRoot);
                        })));
        
        return createOutcome.Bind(aggregateRoot =>
        {
            CreateResourceCommandResponse<TAggregateRoot> response = new(aggregateRoot);
            return Result<CreateResourceCommandResponse<TAggregateRoot>>.Success(response);
        });
    }
}