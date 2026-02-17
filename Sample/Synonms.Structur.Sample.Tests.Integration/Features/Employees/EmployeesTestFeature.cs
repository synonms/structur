using Bogus;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.Schema.Forms;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Core.System;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Sample.Api.Features.Employees.Domain;
using Synonms.Structur.Sample.Api.Features.Employees.Domain.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Testing;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Employees;

public class EmployeesTestFeature : 
    IGetAllTestFeature<Employee, EmployeeResource>, 
    IGetByIdTestFeature<Employee, EmployeeResource>,
    IPostTestFeature<Employee, EmployeeResource>,
    IPutTestFeature<Employee, EmployeeResource>,
    IDeleteTestFeature<Employee>,
    ICreateFormTestFeature,
    IEditFormTestFeature<Employee>
{
    public string CollectionPath => "employees";
    private readonly IUserActionProvider _userActionProvider = new EmptyUserActionProvider();

    public EmployeeResource GenerateInvalidResource(EntityId<Employee> id) =>
        new Faker<EmployeeResource>("en_GB")
            .CustomInstantiator(faker => new EmployeeResource(id.Value, Link.EmptyLink()))
            .RuleFor(x => x.EmployeeReference, string.Empty)
            .RuleFor(x => x.NationalInsuranceNumber, string.Empty)
            .RuleFor(x => x.Forename, string.Empty)
            .RuleFor(x => x.Surname, string.Empty)
            .RuleFor(x => x.HomeAddress, new AddressValueObjectResource { Type = AddressTypeEnumeration.Unknown, Line1 = string.Empty, Postcode = string.Empty})
            .RuleFor(x => x.EmailContacts, [])
            .RuleFor(x => x.TelephoneContacts, []);

    public EmployeeResource GenerateValidResource(EntityId<Employee> id) =>
        new Faker<EmployeeResource>("en_GB")
            .CustomInstantiator(faker => new EmployeeResource(id.Value, Link.EmptyLink()))
            .RuleFor(x => x.EmployeeReference, faker => faker.Random.AlphaNumeric(10))
            .RuleFor(x => x.NationalInsuranceNumber, faker => faker.Random.String(2, 'A', 'C') + faker.Random.Number(100000, 999999) + faker.Random.String(1, 'A', 'D'))
            .RuleFor(x => x.Forename, faker => faker.Name.FirstName())
            .RuleFor(x => x.Surname, faker => faker.Name.LastName())
            .RuleFor(x => x.HomeAddress, faker => new AddressValueObjectResource
            {
                Type = faker.PickRandom(AddressTypeEnumeration.Home, AddressTypeEnumeration.Work), 
                Line1 = faker.Address.StreetAddress(), 
                Postcode = faker.Address.ZipCode("??###??")
            })
            .RuleFor(x => x.EmailContacts, (faker, individualResource) => [
                new EmailContactValueObjectResource
                {
                    Address = faker.Internet.Email(individualResource.Forename, individualResource.Surname),
                    IsPrimary = true
                }
            ])
            .RuleFor(x => x.TelephoneContacts, []);

    public ArrangeAggregateInfo<Employee> GenerateUniqueAggregate(EntityId<Employee> id)
    {
        EmployeeResource resource = GenerateValidResource(id);
        EmployeeCreatedEvent createdEvent = new(_userActionProvider, id, resource, TestTenant.Id);
        Result<Employee> createdResult = createdEvent.ApplyAsync(null).Result;
            
        return createdResult.Match(
            createdIndividual => new ArrangeAggregateInfo<Employee>(createdIndividual),
            errors => throw new ApplicationException($"Unable to create Individual Id '{createdEvent.AggregateId}': {errors}"));
    }
    
    public async Task<Employee> PersistAggregateAsync(IServiceScopeFactory serviceScopeFactory, ArrangeAggregateInfo<Employee> arrangeAggregateInfo)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        IMongoCollection<Employee> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Employee>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Employee)]);

        await collection.InsertOneAsync(arrangeAggregateInfo.AggregateRoot, cancellationToken: TestContext.Current.CancellationToken);

        return arrangeAggregateInfo.AggregateRoot;
    }
    
    public Task PersistPrerequisitesAsync(ArrangeEntitiesInfo arrangeEntitiesInfo) => 
        Task.CompletedTask;

    public async Task<Employee?> RetrieveAggregateAsync(IServiceScopeFactory serviceScopeFactory, EntityId<Employee> id)
    {
        IServiceScope scope = serviceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        IMongoCollection<Employee> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<Employee>(SampleDatabase.MongoDatabaseConfiguration.CollectionNamesByAggregateType[typeof(Employee)]);

        Employee? individual = (await collection.FindAsync(x => x.Id == id)).FirstOrDefault();

        return individual;
    }

    public void ValidateResource(Employee aggregateRoot, EmployeeResource resource)
    {
        Assert.Equal(aggregateRoot.Id.Value, resource.Id);
        Assert.Equal(aggregateRoot.EmployeeReference, resource.EmployeeReference);
        Assert.Equal(aggregateRoot.NationalInsuranceNumber, resource.NationalInsuranceNumber);
        Assert.Equal(aggregateRoot.Title?.Value, resource.Title?.ToString());
        Assert.Equal(aggregateRoot.Forename, resource.Forename);
        Assert.Equal(aggregateRoot.MiddleNames?.Value, resource.MiddleNames);
        Assert.Equal(aggregateRoot.Surname, resource.Surname);
        Assert.Equal(aggregateRoot.KnownAs?.Value, resource.KnownAs);
        Assert.Equal(aggregateRoot.WorkPermitRequired, resource.WorkPermitRequired);
        Assert.Equal(aggregateRoot.WorkPermitValidUntil?.Value, resource.WorkPermitValidUntil);
        Assert.Equal(aggregateRoot.Notes?.Value, resource.Notes);
        Assert.Equal(aggregateRoot.HomeAddress.Type, resource.HomeAddress.Type.ToString());
        Assert.Equal(aggregateRoot.HomeAddress.Line1, resource.HomeAddress.Line1);
        Assert.Equal(aggregateRoot.HomeAddress.Line2?.Value, resource.HomeAddress.Line2);
        Assert.Equal(aggregateRoot.HomeAddress.Line3?.Value, resource.HomeAddress.Line3);
        Assert.Equal(aggregateRoot.HomeAddress.Line4?.Value, resource.HomeAddress.Line4);
        Assert.Equal(aggregateRoot.HomeAddress.Postcode, resource.HomeAddress.Postcode);
        Assert.Equal(aggregateRoot.HomeAddress.Label?.Value, resource.HomeAddress.Label);
        foreach (EmailContact emailAddress in aggregateRoot.EmailContacts)
        {
            Assert.Contains(resource.EmailContacts, x => x.Address == emailAddress.Address);
        }
        foreach (TelephoneContact telephoneNumber in aggregateRoot.TelephoneContacts)
        {
            Assert.Contains(resource.TelephoneContacts, x => x.Number == telephoneNumber.Number);
        }
    }

    public void ValidateCreatedAggregate(Employee aggregateRoot, EmployeeResource resource)
    {
        ValidateCommonAggregateProperties(aggregateRoot, resource);

        Assert.Equal(resource.Id, aggregateRoot.Id.Value);
        Assert.Equal(resource.EmployeeReference, aggregateRoot.EmployeeReference);
        Assert.Equal(resource.NationalInsuranceNumber, aggregateRoot.NationalInsuranceNumber);
    }

    public void ValidateUpdatedAggregate(Employee aggregateRoot, EmployeeResource resource)
    {
        ValidateCommonAggregateProperties(aggregateRoot, resource);
    }

    public void ValidateCreateForm(Form form)
    {
        FormField? forenameField = form.Fields.SingleOrDefault(formField =>
            formField.Name.Equals(nameof(EmployeeResource.Forename).ToCamelCase()));
        
        Assert.NotNull(forenameField);
        Assert.True(forenameField.IsRequired);
        Assert.Equal(Employee.ForenameMaxLength, forenameField.MaxLength);
        
        // TODO: Remaining fields;
    }

    public void ValidateEditForm(Form form, Employee aggregateRoot)
    {
        FormField? forenameField = form.Fields.SingleOrDefault(formField =>
            formField.Name.Equals(nameof(EmployeeResource.Forename).ToCamelCase()));
        
        Assert.NotNull(forenameField);
        Assert.True(forenameField.IsRequired);
        Assert.Equal(Employee.ForenameMaxLength, forenameField.MaxLength);
        Assert.Equal(aggregateRoot.Forename.Value, forenameField.Value);
        
        // TODO: Remaining fields;
    }

    private void ValidateCommonAggregateProperties(Employee aggregateRoot, EmployeeResource resource)
    {
        Assert.Equal(resource.Id, aggregateRoot.Id.Value);
        Assert.Equal(resource.Title?.ToString(), aggregateRoot.Title?.Value);
        Assert.Equal(resource.Forename, aggregateRoot.Forename);
        Assert.Equal(resource.MiddleNames, aggregateRoot.MiddleNames?.Value);
        Assert.Equal(resource.Surname, aggregateRoot.Surname);
        Assert.Equal(resource.KnownAs, aggregateRoot.KnownAs?.Value);
        Assert.Equal(resource.WorkPermitRequired, aggregateRoot.WorkPermitRequired);
        Assert.Equal(resource.WorkPermitValidUntil, aggregateRoot.WorkPermitValidUntil?.Value);
        Assert.Equal(resource.Notes, aggregateRoot.Notes?.Value);
        Assert.Equal(resource.HomeAddress.Type.ToString(), aggregateRoot.HomeAddress.Type);
        Assert.Equal(resource.HomeAddress.Line1, aggregateRoot.HomeAddress.Line1);
        Assert.Equal(resource.HomeAddress.Line2, aggregateRoot.HomeAddress.Line2?.Value);
        Assert.Equal(resource.HomeAddress.Line3, aggregateRoot.HomeAddress.Line3?.Value);
        Assert.Equal(resource.HomeAddress.Line4, aggregateRoot.HomeAddress.Line4?.Value);
        Assert.Equal(resource.HomeAddress.Postcode, aggregateRoot.HomeAddress.Postcode);
        Assert.Equal(resource.HomeAddress.Label, aggregateRoot.HomeAddress.Label?.Value);
        foreach (EmailContactValueObjectResource emailAddress in resource.EmailContacts)
        {
            Assert.Contains(aggregateRoot.EmailContacts, x => x.Address == emailAddress.Address);
        }
        foreach (TelephoneContactValueObjectResource telephoneNumber in resource.TelephoneContacts)
        {
            Assert.Contains(aggregateRoot.TelephoneContacts, x => x.Number == telephoneNumber.Number);
        }
    }
}