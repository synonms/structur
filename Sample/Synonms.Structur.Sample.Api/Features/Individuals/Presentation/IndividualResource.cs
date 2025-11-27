using Synonms.Structur.Application.Schema;
using Synonms.Structur.Application.Schema.Resources;
using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;

namespace Synonms.Structur.Sample.Api.Features.Individuals.Presentation;

public class IndividualResource : Resource
{
    public IndividualResource()
    {
    }

    public IndividualResource(Guid id, Link selfLink)
        : base(id, selfLink)
    {
    }

    [StructurRequired]
    public string TenantReference { get; set; } = string.Empty;

    [StructurImmutable]
    public string FriendlyId { get; set; } = string.Empty;
    
    public string? Salutation { get; set; }

    [StructurRequired]
    [StructurMaxLength(Individual.ForenameMaxLength)]
    public string Forename { get; set; } = string.Empty;

    [StructurRequired]
    [StructurMaxLength(Individual.SurnameMaxLength)]
    public string Surname { get; set; } = string.Empty;
    
    public List<EmailAddressResource> EmailAddresses { get; set; } = [];

    public List<TelephoneNumberResource> TelephoneNumbers { get; set; } = [];
}