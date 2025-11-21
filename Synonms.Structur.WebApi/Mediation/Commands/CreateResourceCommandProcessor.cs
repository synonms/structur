using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.Faults;
using Synonms.Structur.WebApi.Domain;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class CreateResourceCommandProcessor<TAggregateRoot, TResource> : ICommandHandler<CreateResourceCommand<TAggregateRoot, TResource>, CreateResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IDomainEventRepository _domainEventRepository;
    private readonly IAggregateRepository<TAggregateRoot> _aggregateRepository;
    private readonly IDomainEventFactory<TAggregateRoot, TResource> _domainEventFactory;

    public CreateResourceCommandProcessor(IDomainEventRepository domainEventRepository, IAggregateRepository<TAggregateRoot> aggregateRepository, IDomainEventFactory<TAggregateRoot, TResource> domainEventFactory)
    {
        _domainEventRepository = domainEventRepository;
        _aggregateRepository = aggregateRepository;
        _domainEventFactory = domainEventFactory;
    }

    public async Task<Result<CreateResourceCommandResponse<TAggregateRoot>>> HandleAsync(CreateResourceCommand<TAggregateRoot, TResource> command, CancellationToken cancellationToken)
    {
        Maybe<TAggregateRoot> existingOutcome = await _aggregateRepository.FindAsync((EntityId<TAggregateRoot>)command.Resource.Id, cancellationToken);

        Result<TAggregateRoot> createOutcome = await existingOutcome.MatchAsync(
            existingAggregate => Result<TAggregateRoot>.FailureAsync(new DomainRuleFault("{entityType} Id '{id}' already exists.", nameof(TAggregateRoot), command.Resource.Id)), 
            async () =>
            {
                DomainEvent<TAggregateRoot> createdEvent = _domainEventFactory.GenerateCreatedEvent(command.Resource);

                return await createdEvent.ApplyAsync(null)
                    .BindAsync(async aggregateRoot => await _domainEventRepository.CreateAsync(createdEvent, cancellationToken)
                        .ToResultAsync(async () =>
                        {
                            await _aggregateRepository.AddAsync(aggregateRoot, cancellationToken);

                            return Result<TAggregateRoot>.Success(aggregateRoot);
                        }));
            });
        
        return createOutcome.Bind(aggregateRoot =>
        {
            CreateResourceCommandResponse<TAggregateRoot> response = new(aggregateRoot);
            return Result<CreateResourceCommandResponse<TAggregateRoot>>.Success(response);
        });
    }
}