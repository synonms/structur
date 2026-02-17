using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.Schema.Resources;
using Synonms.Structur.Api.Server.Lookups;
using Synonms.Structur.Api.Server.Schema.Forms;
using Synonms.Structur.Domain.Aggregates;

namespace Synonms.Structur.Api.Server.Schema.Resources;

public static class ResourceExtensions
{
    public static Form GenerateCreateForm<TAggregateRoot, TResource>(this TResource resource, Uri targetUri, ILookupOptionsProvider lookupOptionsProvider)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
        where TResource : Resource, new()
    {
        Link targetLink = Link.CreateFormTargetLink(targetUri);

        List<FormField> formFields = resource.GetFormFields(lookupOptionsProvider).ToList();

        return new Form(targetLink, formFields);
    }
    
    public static Form GenerateEditForm<TAggregateRoot, TResource>(this TResource resource, Uri targetUri, ILookupOptionsProvider lookupOptionsProvider)
        where TAggregateRoot : AggregateRoot<TAggregateRoot>
        where TResource : Resource, new()
    {
        Link targetLink = Link.EditFormTargetLink(targetUri);

        List<FormField> formFields = resource.GetFormFields(lookupOptionsProvider).ToList();

        return new Form(targetLink, formFields);
    }
}