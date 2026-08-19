using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Mapping;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Mediation.Queries;

public class FindResourceQueryProcessor<TAggregateRoot, TResource> : IQueryHandler<FindResourceQuery<TAggregateRoot, TResource>, FindResourceQueryResponse<TAggregateRoot, TResource>>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource
{
    private readonly IReadAggregateRepository<TAggregateRoot> _readAggregateRepository;
    private readonly IResourceMapper<TAggregateRoot, TResource> _resourceMapper;

    public FindResourceQueryProcessor(IReadAggregateRepository<TAggregateRoot> readAggregateRepository, IResourceMapper<TAggregateRoot, TResource> resourceMapper)
    {
        _readAggregateRepository = readAggregateRepository;
        _resourceMapper = resourceMapper;
    }
    
    public async Task<Result<FindResourceQueryResponse<TAggregateRoot, TResource>>> HandleAsync(FindResourceQuery<TAggregateRoot, TResource> query, CancellationToken cancellationToken)
    {
        Maybe<TAggregateRoot> findOutcome = await _readAggregateRepository.FindAsync(query.Id, cancellationToken);
            
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