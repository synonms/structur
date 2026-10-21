using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Persistence;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Features.Employees;

[StructurChildResource(typeof(EmployeeEqualOpportunitiesResource))]
[GenerateStorage("employee-equal-opportunities", 1)]
public partial class EmployeeEqualOpportunities : AggregateMember<EmployeeEqualOpportunities>
{
    private EmployeeEqualOpportunities()
    {
    }
    
    private EmployeeEqualOpportunities(EntityId<EmployeeEqualOpportunities> id, EventDate birthDate, Sex sex)
        : base(id)
    {
        BirthDate = birthDate;
        Sex = sex;
    }

    [StoredState("BirthDate")]
    public EventDate BirthDate { get; private set; } = null!;

    [StoredState("Sex")]
    public Sex Sex { get; private set; } = null!;

    internal Maybe<Fault> Update(EmployeeEqualOpportunitiesResource resource, Action rootUpdatedAction) =>
        Validator.CreateBuilder<EmployeeEqualOpportunities>()
            .WithMandatoryScalarProperty(resource.Sex.ToString(), x => Sex.CreateMandatory(nameof(Sex), x), out Sex sexValueObject)
            .Build()
            .BiBind(() => 
            {
                UpdateMandatoryValue(x => x.Sex, sexValueObject, rootUpdatedAction);

                return Maybe<Fault>.None;
            });

    internal static OneOf<EmployeeEqualOpportunities, IEnumerable<DomainRuleFault>> Create(string parentPropertyName, EmployeeEqualOpportunitiesResource resource, Guid id) =>
        Validator.CreateBuilder<EmployeeEqualOpportunities>()
            .WithMandatoryScalarProperty(resource.BirthDate, x => EventDate.CreateMandatory($"{parentPropertyName}.{nameof(BirthDate)}", x), out EventDate birthDateValueObject)
            .WithMandatoryScalarProperty(resource.Sex.ToString(), x => Sex.CreateMandatory($"{parentPropertyName}.{nameof(Sex)}", x), out Sex sexValueObject)
            .Build()
            .ToOneOf(() => new EmployeeEqualOpportunities((EntityId<EmployeeEqualOpportunities>)id, birthDateValueObject, sexValueObject));
}