using Synonms.Structur.Application.Lookups;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Domain.Entities;

namespace Synonms.Structur.Application.Schema.Forms;

public class EditFormDocumentFactory<TAggregateRoot, TResource> : IEditFormDocumentFactory<TAggregateRoot, TResource>
    where TAggregateRoot : AggregateRoot<TAggregateRoot>
    where TResource : Resource, new()
{
    private readonly ILookupOptionsProvider _lookupOptionsProvider;

    public EditFormDocumentFactory(ILookupOptionsProvider lookupOptionsProvider)
    {
        _lookupOptionsProvider = lookupOptionsProvider;
    }

    public FormDocument Create(Uri documentUri, Uri targetUri, TResource resource)
    {
        Form form = resource.GenerateEditForm<TAggregateRoot, TResource>(targetUri, _lookupOptionsProvider);
        Link selfLink = Link.SelfLink(documentUri);
        
        return new FormDocument(selfLink, form);
    }
}