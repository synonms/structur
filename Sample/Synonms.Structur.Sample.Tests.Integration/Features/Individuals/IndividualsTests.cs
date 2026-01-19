using System.Net;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Individuals;

public class IndividualsTests(SampleTestFixture fixture)
{
    private readonly IndividualsTestFeature _testFeature = new();
        
    [Fact]
    public void CreateForm_Valid_Returns200Ok() =>
        CreateFormTest.Create(fixture, _testFeature)
            .Arrange
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);

    [Fact]
    public void Delete_KnownId_Returns200Ok() =>
        DeleteTest<Individual>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void Delete_UnknownId_Returns404NotFound() =>
        DeleteTest<Individual>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void EditForm_KnownId_Returns200Ok() =>
        EditFormTest<Individual>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void EditForm_UnknownId_Returns404NotFound() =>
        EditFormTest<Individual>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void GetAll_EntitiesExist_Returns200OkIncludingEntities() =>
        GetAllTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregates(2)
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);

    [Fact]
    public void GetAll_NoEntitiesExist_Returns200OkWithEmptyCollection() =>
        GetAllTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregates()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_KnownId_Returns200Ok() =>
        GetByIdTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregate()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_UnknownId_Returns404NotFound() =>
        GetByIdTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
    
    [Fact]
    public void Post_Invalid_Returns400BadRequest() =>
        PostTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithInvalidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.BadRequest);

    [Fact]
    public void Post_Valid_Returns200OkWithLocation() =>
        PostTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithValidResource()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.Created);
    
    [Fact]
    public void Put_KnownIdWithValidResource_Returns204NoContent() =>
        PutTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange
                .WithAggregate()
                .WithValidResource()
            .Act
            .Assert.SucceedsWith(HttpStatusCode.NoContent);

    [Fact]
    public void Put_KnownIdWithInvalidResource_Returns400BadRequest() =>
        PutTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange
                .WithAggregate()
                .WithInvalidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.BadRequest);

    [Fact]
    public void Put_UnknownId_Returns404NotFound() =>
        PutTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange
                .WithoutAggregate()
                .WithValidResource()
            .Act
            .Assert.FailsWith(HttpStatusCode.NotFound);
}