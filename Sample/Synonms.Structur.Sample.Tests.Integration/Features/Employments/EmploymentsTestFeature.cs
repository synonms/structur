using Bogus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employees.Events;
using Synonms.Structur.Sample.Api.Features.Employments;
using Synonms.Structur.Sample.Api.Features.Employments.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Sample.ClientApi.Features.Employments;
using Synonms.Structur.Testing;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Employments;

public class EmploymentsTestFeature :
    IGetAllTestFeature<Employment, EmploymentResource>,
    IGetByIdTestFeature<Employment, EmploymentResource>,
    IPostTestFeature<Employment, EmploymentResource>,
    IPutTestFeature<Employment, EmploymentResource>,
    ICreateFormTestFeature,
    IEditFormTestFeature<Employment>
{
    public string CollectionPath => "employments";
    private readonly IUserActionProvider _userActionProvider = new EmptyUserActionProvider();
    private readonly IVersionContext _versionContext = new VersionContext();

    public EmploymentResource GenerateInvalidResource(EntityId<Employment> id) =>
        new Faker<EmploymentResource>("en_GB")
            .CustomInstantiator(faker => new EmploymentResource(id.Value, Link.EmptyLink()))
            .RuleFor(x => x.EmployeeId, Guid.Empty)
            .RuleFor(x => x.EmployeeNumber, string.Empty)
            .RuleFor(x => x.ContinuousStartDate, DateOnly.MinValue)
            .RuleFor(x => x.Contracts, [])
            .RuleFor(x => x.BankDetails, new UkBankDetailsResource { BankName = string.Empty, SortCode = string.Empty, AccountNumber = string.Empty, AccountName = string.Empty });

    public EmploymentResource GenerateValidResource(EntityId<Employment> id) =>
        new Faker<EmploymentResource>("en_GB")
            .CustomInstantiator(faker => new EmploymentResource(id.Value, Link.EmptyLink()))
            .RuleFor(x => x.EmployeeId, Guid.NewGuid())
            .RuleFor(x => x.EmployeeNumber, faker => $"EMP{faker.Random.Number(1000, 9999)}")
            .RuleFor(x => x.ContinuousStartDate, faker => DateOnly.FromDateTime(faker.Date.Past(5)))
            .RuleFor(x => x.Contracts, faker => [
                new EmploymentContractResource(Guid.NewGuid())
                {
                    StartDate = DateOnly.FromDateTime(faker.Date.Past(5)),
                    EmployerNoticePeriod = new PeriodResource { Units = 1, Interval = "Month" },
                    EmployeeNoticePeriod = new PeriodResource { Units = 1, Interval = "Month" },
                    Position = "Developer",
                    Location = "Office",
                    CanClaimTravelExpensesToOffice = false
                }
            ])
            .RuleFor(x => x.BankDetails, faker => new UkBankDetailsResource
            {
                BankName = faker.Company.CompanyName(),
                SortCode = "12-34-56",
                AccountNumber = "12345678",
                AccountName = faker.Name.FullName()
            });

    public ArrangeAggregateInfo<Employment> GenerateUniqueAggregate(EntityId<Employment> id)
    {
        EmploymentResource resource = GenerateValidResource(id);
        EmploymentCreatedEvent createdEvent = new(_userActionProvider, _versionContext, id, resource, TestTenant.Id);
        Result<Employment> createdResult = createdEvent.ApplyAsync(null).Result;

        return createdResult.Match(
            createdEmployment => new ArrangeAggregateInfo<Employment>(createdEmployment),
            errors => throw new ApplicationException($"Unable to create Employment Id '{createdEvent.AggregateId}': {errors}"));
    }

    public async Task<Employment> PersistAggregateAsync(IServiceScopeFactory serviceScopeFactory, ArrangeAggregateInfo<Employment> arrangeAggregateInfo)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();

        IMongoCollection<Employment> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Employment>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Employment)]);

        await collection.InsertOneAsync(arrangeAggregateInfo.AggregateRoot, cancellationToken: TestContext.Current.CancellationToken);

        return arrangeAggregateInfo.AggregateRoot;
    }

    public async Task PersistPrerequisitesAsync(IServiceScopeFactory serviceScopeFactory, ArrangeEntitiesInfo arrangeEntitiesInfo)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        IMongoCollection<Employee> employeesCollection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Employee>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Employee)]);

        foreach (Employee employee in arrangeEntitiesInfo.Entities?.Where(e => e is Employee).Cast<Employee>() ?? [])
        {
            await employeesCollection.InsertOneAsync(employee, cancellationToken: TestContext.Current.CancellationToken);
        }
    }

    public async Task<Employment?> RetrieveAggregateAsync(IServiceScopeFactory serviceScopeFactory, EntityId<Employment> id)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();

        IMongoCollection<Employment> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Employment>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Employment)]);

        Employment? employment = (await collection.FindAsync(x => x.Id == id)).FirstOrDefault();

        return employment;
    }

    public void ValidateResource(Employment aggregateRoot, EmploymentResource resource)
    {
        Assert.Equal(aggregateRoot.Id.Value, resource.Id);
        Assert.Equal(aggregateRoot.EmployeeId.Value, resource.EmployeeId);
        Assert.Equal(aggregateRoot.EmployeeNumber, resource.EmployeeNumber);
        Assert.Equal(aggregateRoot.ContinuousStartDate, resource.ContinuousStartDate);
        Assert.Equal(aggregateRoot.BankDetails.BankName, resource.BankDetails.BankName);
        Assert.Equal(aggregateRoot.BankDetails.SortCode, resource.BankDetails.SortCode);
        Assert.Equal(aggregateRoot.BankDetails.AccountNumber, resource.BankDetails.AccountNumber);
        Assert.Equal(aggregateRoot.BankDetails.AccountName, resource.BankDetails.AccountName);
        Assert.Equal(aggregateRoot.BankDetails.BuildingSocietyRollNumber?.Value, resource.BankDetails.BuildingSocietyRollNumber);
        // Validate contracts if needed
        Assert.Equal(aggregateRoot.Contracts.Count, resource.Contracts.Count);
        for (int i = 0; i < aggregateRoot.Contracts.Count; i++)
        {
            EmploymentContract contract = aggregateRoot.Contracts[i];
            EmploymentContractResource contractResource = resource.Contracts[i];
            Assert.Equal(contract.Id.Value, contractResource.Id);
            Assert.Equal(contract.StartDate, contractResource.StartDate);
            Assert.Equal(contract.ProbationEndDate?.Value, contractResource.ProbationEndDate);
            Assert.Equal(contract.EndDate?.Value, contractResource.EndDate);
            Assert.Equal(contract.EmployerNoticePeriod.Units, contractResource.EmployerNoticePeriod.Units);
            Assert.Equal(contract.EmployerNoticePeriod.Interval, contractResource.EmployerNoticePeriod.Interval);
            Assert.Equal(contract.EmployeeNoticePeriod.Units, contractResource.EmployeeNoticePeriod.Units);
            Assert.Equal(contract.EmployeeNoticePeriod.Interval, contractResource.EmployeeNoticePeriod.Interval);
            Assert.Equal(contract.Position, contractResource.Position);
            Assert.Equal(contract.Location, contractResource.Location);
            Assert.Equal(contract.LocationNotes?.Value, contractResource.LocationNotes);
            Assert.Equal(contract.ReportsToEmployeeId?.Value, contractResource.ReportsToEmployeeId);
            Assert.Equal(contract.CarRegistrationPlate?.Value, contractResource.CarRegistrationPlate);
            Assert.Equal(contract.Notes?.Value, contractResource.Notes);
            Assert.Equal(contract.CanClaimTravelExpensesToOffice, contractResource.CanClaimTravelExpensesToOffice);
        }
    }

    public void ValidateCreatedAggregate(Employment aggregateRoot, EmploymentResource resource)
    {
        ValidateResource(aggregateRoot, resource);
    }

    public void ValidateUpdatedAggregate(Employment aggregateRoot, EmploymentResource resource)
    {
        ValidateResource(aggregateRoot, resource);
    }

    public void ValidateCreateForm(Form form)
    {
        // TODO: Implement form validation if needed
    }

    public void ValidateEditForm(Form form, Employment aggregateRoot)
    {
        // TODO: Implement form validation if needed
    }
    
    private async Task<Employee> CreateEmployeeAsync()
    {
        EntityId<Employee> employeeId = EntityId<Employee>.New();
        EmployeeResource employeeResource = new Faker<EmployeeResource>("en_GB")
            .CustomInstantiator(faker => new EmployeeResource(employeeId.Value, Link.EmptyLink()))
            .RuleFor(x => x.EmployeeReference, faker => faker.Random.AlphaNumeric(10))
            .RuleFor(x => x.NationalInsuranceNumber, faker => faker.Random.String(2, 'A', 'C') + faker.Random.Number(100000, 999999) + faker.Random.String(1, 'A', 'D'))
            .RuleFor(x => x.Forename, faker => faker.Name.FirstName())
            .RuleFor(x => x.Surname, faker => faker.Name.LastName())
            .RuleFor(x => x.HomeAddress, faker => new AddressResource
            {
                Type = AddressTypeEnumeration.Home,
                Line1 = faker.Address.StreetAddress(),
                Postcode = faker.Address.ZipCode("??###??")
            })
            .RuleFor(x => x.EmailContacts, (faker, individualResource) => [
                new EmailContactResource
                {
                    Address = faker.Internet.Email(individualResource.Forename, individualResource.Surname),
                    IsPrimary = true
                }
            ])
            .RuleFor(x => x.TelephoneContacts, []);

        EmployeeCreatedEvent employeeCreatedEvent = new(_userActionProvider, _versionContext, employeeId, employeeResource, TestTenant.Id);
        Result<Employee> employeeResult = await employeeCreatedEvent.ApplyAsync(null);
        Employee employee = employeeResult.Match(
            e => e,
            errors => throw new ApplicationException($"Unable to create test Employee: {errors}"));

        return employee;
    }
}
