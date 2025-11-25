using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Synonms.Structur.Core.Cqrs;
using Synonms.Structur.Core.Functional;
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
    private readonly ICommandHandler<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>> _commandHandler;

    public DeleteEndpoint(ICommandHandler<DeleteResourceCommand<TAggregateRoot>, DeleteResourceCommandResponse<TAggregateRoot>> commandHandler)
    {
        _commandHandler = commandHandler;
    }
    
    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteAsync([FromRoute] EntityId<TAggregateRoot> id)
    {
        // TODO: Support parameters
        DeleteResourceCommand<TAggregateRoot> request = new(id);
        Result<DeleteResourceCommandResponse<TAggregateRoot>> response = await _commandHandler.HandleAsync(request);

        return response.Match<IActionResult>(
            _ => StatusCode(StatusCodes.Status204NoContent),
            fault => HttpResponseMapper.MapFault(fault));
    }
}