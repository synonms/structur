using System.Net;
using Synonms.Structur.Sample.Api.Features.Employees.Domain;
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
}