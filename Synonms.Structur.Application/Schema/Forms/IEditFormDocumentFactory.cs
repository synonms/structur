using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Schema.Forms;

public interface IEditFormDocumentFactory<TAggregateRoot, in TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new()
{
    FormDocument Create(Uri documentUri, Uri targetUri, TResource resource);
}