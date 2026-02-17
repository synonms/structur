using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Schema.Forms;

public interface IEditFormDocumentFactory<TAggregateRoot, in TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new()
{
    FormDocument Create(Uri documentUri, Uri targetUri, TResource resource);
}