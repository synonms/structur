using System.Net;
using Synonms.Structur.Domain.Entities;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Features.Individuals.Presentation;
using Synonms.Structur.Testing.Tests;

namespace Synonms.Structur.Sample.Tests.Integration.Features.Individuals;

public class IndividualsTests(SampleTestFixture fixture)
{
    private readonly IndividualsTestFeature _testFeature = new();
    
    [Fact]
    public void GetById_KnownId_Returns200Ok() =>
        GetByIdTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithAggregate(_testFeature.GenerateUniqueAggregate())
            .Act
            .Assert.SucceedsWith(HttpStatusCode.OK);
    
    [Fact]
    public void GetById_UnknownId_Returns404NotFound() =>
        GetByIdTest<Individual, IndividualResource>.Create(fixture, _testFeature)
            .Arrange.WithoutAggregate()
            .Act.WithId(EntityId<Individual>.New())
            .Assert.FailsWith(HttpStatusCode.NotFound);
}