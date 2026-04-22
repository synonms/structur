using System.Net;
using Synonms.Structur.Sample.Api.Features.Employments;
using Synonms.Structur.Sample.ClientApi.Features.Employments;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Employments;

public class EmploymentsTests(SampleTestFixture fixture)
{
    private readonly EmploymentsTestFeature _testFeature = new();
        
    [Fact]
    public void CreateForm_Valid_Returns200Ok() =>
        CreateFormTest.Create(fixture, _testFeature)
            .Arrange
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);

    [Fact]
    public void EditForm_KnownId_Returns200Ok() =>
        EditFormTest<Employment>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void EditForm_UnknownId_Returns404NotFound() =>
        EditFormTest<Employment>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void GetAll_EntitiesExist_Returns200OkIncludingEntities() =>
        GetAllTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregates(2)
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);

    [Fact]
    public void GetAll_NoEntitiesExist_Returns200OkWithEmptyCollection() =>
        GetAllTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregates()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_KnownId_Returns200Ok() =>
        GetByIdTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_UnknownId_Returns404NotFound() =>
        GetByIdTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void Post_Invalid_Returns400BadRequest() =>
        PostTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange.WithInvalidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.BadRequest);

    [Fact]
    public void Post_Valid_Returns200OkWithLocation() =>
        PostTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange.WithValidResource()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.Created);
    
    [Fact]
    public void Put_KnownIdWithValidResource_Returns204NoContent() =>
        PutTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange
                .WithAggregate()
                .WithValidResource()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.NoContent);

    [Fact]
    public void Put_KnownIdWithInvalidResource_Returns400BadRequest() =>
        PutTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange
                .WithAggregate()
                .WithInvalidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.BadRequest);

    [Fact]
    public void Put_UnknownId_Returns404NotFound() =>
        PutTest<Employment, EmploymentResource>.Create(fixture, _testFeature)
            .Arrange
                .WithoutAggregate()
                .WithValidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
}
