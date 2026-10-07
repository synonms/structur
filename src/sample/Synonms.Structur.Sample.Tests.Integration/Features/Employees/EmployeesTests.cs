using System.Net;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Aggregates;
using Synonms.Structur.Infrastructure.MongoDb.Aggregates;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employees.Persistence;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Employees;

public class EmployeesTests(SampleTestFixture fixture)
{
    private readonly EmployeesTestFeature _testFeature = new();
        
    [Fact]
    public void CreateForm_Valid_Returns200Ok() =>
        CreateFormTest.Create(fixture, _testFeature)
            .Arrange
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);

    [Fact]
    public void Delete_KnownId_Returns200Ok() =>
        DeleteTest<Employee>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void Delete_UnknownId_Returns404NotFound() =>
        DeleteTest<Employee>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void EditForm_KnownId_Returns200Ok() =>
        EditFormTest<Employee>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void EditForm_UnknownId_Returns404NotFound() =>
        EditFormTest<Employee>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void GetAll_EntitiesExist_Returns200OkIncludingEntities() =>
        GetAllTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregates(2)
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);

    [Fact]
    public void GetAll_NoEntitiesExist_Returns200OkWithEmptyCollection() =>
        GetAllTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregates()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_KnownId_Returns200Ok() =>
        GetByIdTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_UnknownId_Returns404NotFound() =>
        GetByIdTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void Post_Invalid_Returns400BadRequest() =>
        PostTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange.WithInvalidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.BadRequest);

    [Fact]
    public void Post_Valid_Returns200OkWithLocation() =>
        PostTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange.WithValidResource()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.Created);
    
    [Fact]
    public void Put_KnownIdWithValidResource_Returns204NoContent() =>
        PutTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange
                .WithAggregate()
                .WithValidResource()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.NoContent);

    [Fact]
    public void Put_KnownIdWithInvalidResource_Returns400BadRequest() =>
        PutTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange
                .WithAggregate()
                .WithInvalidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.BadRequest);

    [Fact]
    public void Put_UnknownId_Returns404NotFound() =>
        PutTest<Employee, EmployeeResource>.Create(fixture, _testFeature)
            .Arrange
                .WithoutAggregate()
                .WithValidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);

    [Fact]
    public async Task WriteRepository_AddAsync_PersistsEmployeeRecord()
    {
        EntityId<Employee> employeeId = EntityId<Employee>.New();
        Employee aggregateRoot = _testFeature.GenerateUniqueAggregate(employeeId).AggregateRoot;

        using IServiceScope scope = fixture.ServiceScopeFactory.CreateScope();
        IWriteAggregateRepository<Employee> writeRepository = scope.ServiceProvider.GetRequiredService<IWriteAggregateRepository<Employee>>();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();

        await writeRepository.AddAsync(aggregateRoot, TestContext.Current.CancellationToken);

        IMongoCollection<EmployeeRecord> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<EmployeeRecord>(SampleDatabase.MongoDatabaseConfiguration.GetCollectionName<Employee>());

        EmployeeRecord? employeeRecord = await collection
            .Find(x => x.Id == employeeId.Value)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(employeeRecord);
        Assert.Equal(aggregateRoot.EmployeeReference, employeeRecord.EmployeeReference);
    }

    [Fact]
    public async Task ReadRepository_FindAsync_RecordMissingOptionalFields_ReturnsAggregate()
    {
        EntityId<Employee> employeeId = EntityId<Employee>.New();
        Employee aggregateRoot = _testFeature.GenerateUniqueAggregate(employeeId).AggregateRoot;
        EmployeeRecord employeeRecord = MongoDbRecordMapper.Map<EmployeeRecord>(aggregateRoot);
        BsonDocument document = employeeRecord.ToBsonDocument();

        document.Remove(nameof(EmployeeRecord.MiddleNames));
        document.Remove(nameof(EmployeeRecord.KnownAs));
        document.Remove(nameof(EmployeeRecord.EmailContacts));
        document.Remove(nameof(EmployeeRecord.TelephoneContacts));
        document.Remove(nameof(EmployeeRecord.EqualOpportunities));

        using IServiceScope scope = fixture.ServiceScopeFactory.CreateScope();
        IReadAggregateRepository<Employee> readRepository = scope.ServiceProvider.GetRequiredService<IReadAggregateRepository<Employee>>();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        IMongoCollection<BsonDocument> collection = mongoClient.GetDatabase(SampleDatabase.DatabaseName)
            .GetCollection<BsonDocument>(SampleDatabase.MongoDatabaseConfiguration.GetCollectionName<Employee>());

        await collection.InsertOneAsync(document, cancellationToken: TestContext.Current.CancellationToken);

        Maybe<Employee> employeeMaybe = await readRepository.FindAsync(employeeId, TestContext.Current.CancellationToken);

        Assert.True(employeeMaybe.IsSome);

        Employee employee = (Employee)employeeMaybe;
        Assert.Null(employee.MiddleNames);
        Assert.Null(employee.KnownAs);
        Assert.Empty(employee.EmailContacts);
        Assert.Empty(employee.TelephoneContacts);
        Assert.Null(employee.EqualOpportunities);
    }
}