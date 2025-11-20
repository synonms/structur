using Synonms.Structur.Core.Mediation;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.WebApi.Mediation.Commands;

public class DeleteResourceCommandResponse<TAggregateRoot> : CommandResponse
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
{
}