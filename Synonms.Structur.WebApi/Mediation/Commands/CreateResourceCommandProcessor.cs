using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.WebApi.Mediation.Commands;

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

        Result<DomainEvent<TAggregateRoot>> generateEventOutcome = existingOutcome
            .Match(
                existingAggregate => Result<DomainEvent<TAggregateRoot>>.Failure(new DomainRuleFault("{entityType} Id '{id}' already exists.", nameof(TAggregateRoot), command.Resource.Id)), 
                () => _domainEventFactory.GenerateCreatedEvent(command.Resource));
        
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