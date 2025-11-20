using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Application.Mapping;
using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Application.Schema.Resources;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class CreateResourceCommandProcessor<TAggregateRoot, TResource> : ICommandHandler<CreateResourceCommand<TAggregateRoot, TResource>, CreateResourceCommandResponse<TAggregateRoot>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IAggregateRepository<TAggregateRoot> _aggregateRepository;
    private readonly IAggregateCreator<TAggregateRoot, TResource> _aggregateCreator;

    public CreateResourceCommandProcessor(IAggregateRepository<TAggregateRoot> aggregateRepository, IAggregateCreator<TAggregateRoot, TResource> aggregateCreator)
    {
        _aggregateRepository = aggregateRepository;
        _aggregateCreator = aggregateCreator;
    }

    public async Task<Result<CreateResourceCommandResponse<TAggregateRoot>>> HandleAsync(CreateResourceCommand<TAggregateRoot, TResource> command, CancellationToken cancellationToken)
    {
        Result<TAggregateRoot> createOutcome = await _aggregateCreator.CreateAsync(command.Resource, cancellationToken);
        
        Result<TAggregateRoot> persistOutcome = await createOutcome
            .BindAsync(
                async aggregateRoot =>
                {
                    await _aggregateRepository.AddAsync(aggregateRoot, cancellationToken);
                    await _aggregateRepository.SaveChangesAsync(cancellationToken);

                    return Result<TAggregateRoot>.Success(aggregateRoot);
                });
        
        return persistOutcome.Bind(aggregateRoot =>
        {
            CreateResourceCommandResponse<TAggregateRoot> response = new(aggregateRoot);
            return Result<CreateResourceCommandResponse<TAggregateRoot>>.Success(response);
        });
    }
}