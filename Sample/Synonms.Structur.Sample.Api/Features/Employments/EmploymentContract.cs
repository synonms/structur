using Synonms.Structur.Core.Attributes;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Faults;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Domain.Validation;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.ClientApi.Features.Employments;

namespace Synonms.Structur.Sample.Api.Features.Employments;

[StructurChildResource(typeof(EmploymentContractResource))]
public class EmploymentContract : AggregateMember<EmploymentContract>
{
    private EmploymentContract()
    {
    }

    private EmploymentContract(
        EntityId<EmploymentContract> id,
        EffectiveDate startDate,
        Period employerNoticePeriod,
        Period employeeNoticePeriod,
        Role position,
        WorkLocation location,
        bool canClaimTravelExpensesToOffice)
        : base(id)
    {
        StartDate = startDate;
        EmployerNoticePeriod = employerNoticePeriod;
        EmployeeNoticePeriod = employeeNoticePeriod;
        Position = position;
        Location = location;
        CanClaimTravelExpensesToOffice = canClaimTravelExpensesToOffice;
    }

    public EffectiveDate StartDate { get; private set; } = null!;

    public EventDate? ProbationEndDate { get; private set; }

    public EffectiveDate? EndDate { get; private set; }

    public Period EmployerNoticePeriod { get; private set; } = null!;

    public Period EmployeeNoticePeriod { get; private set; } = null!;

    public Role Position { get; private set; } = null!;

    public WorkLocation Location { get; private set; } = null!;

    public Notes? LocationNotes { get; private set; }

    public EntityId<Employee>? ReportsToEmployeeId { get; private set; }

    public CarRegistrationPlate? CarRegistrationPlate { get; private set; }

    public Notes? Notes { get; private set; }

    public bool CanClaimTravelExpensesToOffice { get; private set; }

    internal Maybe<Fault> Update(EmploymentContractResource resource, Action rootUpdatedAction) =>
        Validator.CreateBuilder<EmploymentContract>()
            .WithMandatoryScalarProperty(resource.StartDate, x => EffectiveDate.CreateMandatory(nameof(StartDate), x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate startDateValueObject)
            .WithOptionalScalarProperty(resource.ProbationEndDate, x => EventDate.CreateOptional(nameof(ProbationEndDate), x), out EventDate? probationEndDateValueObject)
            .WithOptionalScalarProperty(resource.EndDate, x => EffectiveDate.CreateOptional(nameof(EndDate), x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate? endDateValueObject)
            .WithMandatoryScalarProperty(resource.EmployerNoticePeriod, x => Period.CreateMandatory($"{nameof(EmployerNoticePeriod)}", x.Units, x.Interval), out Period employerNoticePeriodValueObject)
            .WithMandatoryScalarProperty(resource.EmployeeNoticePeriod, x => Period.CreateMandatory($"{nameof(EmployeeNoticePeriod)}", x.Units, x.Interval), out Period employeeNoticePeriodValueObject)
            .WithMandatoryScalarProperty(resource.Position, x => Role.CreateMandatory(nameof(Position), x), out Role positionValueObject)
            .WithMandatoryScalarProperty(resource.Location, x => WorkLocation.CreateMandatory(nameof(Location), x), out WorkLocation locationValueObject)
            .WithOptionalScalarProperty(resource.LocationNotes, x => Notes.CreateOptional(nameof(LocationNotes), x), out Notes? locationNotesValueObject)
            .WithOptionalScalarProperty(resource.CarRegistrationPlate, x => CarRegistrationPlate.CreateOptional(nameof(CarRegistrationPlate), x), out CarRegistrationPlate? carRegistrationPlateValueObject)
            .WithOptionalScalarProperty(resource.Notes, x => Notes.CreateOptional(nameof(Notes), x), out Notes? notesValueObject)
            .Build()
            .BiBind(() =>
            {
                UpdateMandatoryValue(x => x.StartDate, startDateValueObject, rootUpdatedAction);
                UpdateOptionalValue(x => x.ProbationEndDate, probationEndDateValueObject, rootUpdatedAction);
                UpdateOptionalValue(x => x.EndDate, endDateValueObject, rootUpdatedAction);
                UpdateMandatoryValue(x => x.EmployerNoticePeriod, employerNoticePeriodValueObject, rootUpdatedAction);
                UpdateMandatoryValue(x => x.EmployeeNoticePeriod, employeeNoticePeriodValueObject, rootUpdatedAction);
                UpdateMandatoryValue(x => x.Position, positionValueObject, rootUpdatedAction);
                UpdateMandatoryValue(x => x.Location, locationValueObject, rootUpdatedAction);
                UpdateOptionalValue(x => x.LocationNotes, locationNotesValueObject, rootUpdatedAction);
                UpdateOptionalValue(x => x.ReportsToEmployeeId, resource.ReportsToEmployeeId is not null ? (EntityId<Employee>)resource.ReportsToEmployeeId.Value : null, rootUpdatedAction);
                UpdateOptionalValue(x => x.CarRegistrationPlate, carRegistrationPlateValueObject, rootUpdatedAction);
                UpdateOptionalValue(x => x.Notes, notesValueObject, rootUpdatedAction);
                UpdateOptionalValue(x => x.CanClaimTravelExpensesToOffice, resource.CanClaimTravelExpensesToOffice, rootUpdatedAction);

                return Maybe<Fault>.None;
            });

    internal static OneOf<EmploymentContract, IEnumerable<DomainRuleFault>> Create(string parentPropertyName, EmploymentContractResource resource) =>
        Validator.CreateBuilder<EmploymentContract>()
            .WithMandatoryScalarProperty(resource.StartDate, x => EffectiveDate.CreateMandatory($"{parentPropertyName}.{nameof(StartDate)}", x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate startDateValueObject)
            .WithOptionalScalarProperty(resource.ProbationEndDate, x => EventDate.CreateOptional($"{parentPropertyName}.{nameof(ProbationEndDate)}", x), out EventDate? probationEndDateValueObject)
            .WithOptionalScalarProperty(resource.EndDate, x => EffectiveDate.CreateOptional($"{parentPropertyName}.{nameof(EndDate)}", x, DateOnly.Parse("1970-01-01"), DateOnly.MaxValue), out EffectiveDate? endDateValueObject)
            .WithMandatoryScalarProperty(resource.EmployerNoticePeriod, x => Period.CreateMandatory($"{parentPropertyName}.{nameof(EmployerNoticePeriod)}", x.Units, x.Interval), out Period employerNoticePeriodValueObject)
            .WithMandatoryScalarProperty(resource.EmployeeNoticePeriod, x => Period.CreateMandatory($"{parentPropertyName}.{nameof(EmployeeNoticePeriod)}", x.Units, x.Interval), out Period employeeNoticePeriodValueObject)
            .WithMandatoryScalarProperty(resource.Position, x => Role.CreateMandatory($"{parentPropertyName}.{nameof(Position)}", x), out Role positionValueObject)
            .WithMandatoryScalarProperty(resource.Location, x => WorkLocation.CreateMandatory($"{parentPropertyName}.{nameof(Location)}", x), out WorkLocation locationValueObject)
            .WithOptionalScalarProperty(resource.LocationNotes, x => Notes.CreateOptional($"{parentPropertyName}.{nameof(LocationNotes)}", x), out Notes? locationNotesValueObject)
            .WithOptionalScalarProperty(resource.CarRegistrationPlate, x => CarRegistrationPlate.CreateOptional($"{parentPropertyName}.{nameof(CarRegistrationPlate)}", x), out CarRegistrationPlate? carRegistrationPlateValueObject)
            .WithOptionalScalarProperty(resource.Notes, x => Notes.CreateOptional($"{parentPropertyName}.{nameof(Notes)}", x), out Notes? notesValueObject)
            .Build()
            .ToOneOf(() => 
                new EmploymentContract((EntityId<EmploymentContract>)resource.Id, startDateValueObject, employerNoticePeriodValueObject, employeeNoticePeriodValueObject, positionValueObject, locationValueObject, resource.CanClaimTravelExpensesToOffice)
                {
                    ProbationEndDate = probationEndDateValueObject,
                    EndDate = endDateValueObject,
                    LocationNotes = locationNotesValueObject,
                    ReportsToEmployeeId = resource.ReportsToEmployeeId is not null ? (EntityId<Employee>)resource.ReportsToEmployeeId.Value : null,
                    CarRegistrationPlate = carRegistrationPlateValueObject,
                    Notes = notesValueObject
                });
}

