using Synonms.Structur.Application.Mapping;
using Synonms.Structur.Application.Persistence;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Domain.Faults;

namespace Synonms.Structur.WebApi.Mediation.Queries;

public class FindResourceQueryProcessor<TAggregateRoot, TResource> : IQueryHandler<FindResourceQuery<TAggregateRoot, TResource>, FindResourceQueryResponse<TAggregateRoot, TResource>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IAggregateRepository<TAggregateRoot> _aggregateRepository;
    private readonly IResourceMapper<TAggregateRoot, TResource> _resourceMapper;

    public FindResourceQueryProcessor(IAggregateRepository<TAggregateRoot> aggregateRepository, IResourceMapper<TAggregateRoot, TResource> resourceMapper)
    {
        _aggregateRepository = aggregateRepository;
        _resourceMapper = resourceMapper;
    }
    
    public async Task<Result<FindResourceQueryResponse<TAggregateRoot, TResource>>> HandleAsync(FindResourceQuery<TAggregateRoot, TResource> query, CancellationToken cancellationToken)
    {
        Maybe<TAggregateRoot> findOutcome = await _aggregateRepository.FindAsync(query.Id, cancellationToken);
            
        Result<FindResourceQueryResponse<TAggregateRoot, TResource>> response = findOutcome.Match(
            aggregateRoot =>
            {
                TResource resource = _resourceMapper.Map(aggregateRoot);
                FindResourceQueryResponse<TAggregateRoot, TResource> response = new(resource, aggregateRoot.EntityTag);
                return response;
            },
            () =>
            {
                EntityNotFoundFault fault = new("{0} with id '{1}' not found.", nameof(TAggregateRoot), query.Id);
                return Result<FindResourceQueryResponse<TAggregateRoot, TResource>>.Failure(fault);
            });

        return response;
    }
}