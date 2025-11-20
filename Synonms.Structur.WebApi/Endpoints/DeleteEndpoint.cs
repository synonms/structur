using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.WebApi.Cors;
using Synonms.Structur.WebApi.Http;
using Synonms.Structur.WebApi.Mediation.Commands;

namespace Synonms.Structur.WebApi.Endpoints;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[EnableCors(CorsConstants.PolicyName)]
public class DeleteEndpoint<TAggregateRoot> : ControllerBase
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
    private readonly IMediator _mediator;

    public DeleteEndpoint(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        // TODO: Support parameters
        DeleteResourceCommand<TAggregateRoot> request = new(id);
        Result<DeleteResourceCommandResponse<TAggregateRoot>> response = await _mediator.SendCommandAsync<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>>(request);

        return response.Match<IActionResult>(
            _ => StatusCode(StatusCodes.Status204NoContent),
            fault => HttpResponseMapper.MapFault(fault));
    }
}