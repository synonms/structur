using MongoDB.Bson;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Infrastructure.MongoDb.Aggregates;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employments;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.Tests.Integration.Features.Employees;
using Synonms.Structur.Sample.Tests.Integration.Features.Employments;

namespace Synonms.Structur.Sample.Tests.Integration;

public sealed class StorageMigrationTests
{
    [Fact]
    public void GeneratedEmployeeStorageRoundTripsAsPlainBsonAndPreservesTheHistoricalFieldShape()
    {
        Employee employee = new EmployeesTestFeature().GenerateUniqueAggregate(EntityId<Employee>.New()).AggregateRoot;
        MongoDbAggregateRecordMapper<Employee> mapper = new(SampleDatabase.MongoDatabaseConfiguration);

        BsonDocument stored = mapper.Capture(employee);
        Employee restored = mapper.Restore(stored);

        Assert.Equal(employee.Id, restored.Id);
        Assert.Equal(employee.TenantId, restored.TenantId);
        Assert.Equal(employee.EntityTag, restored.EntityTag);
        Assert.Equal(ToBsonDateTime(employee.CreatedAction.ActionAt), restored.CreatedAction.ActionAt);
        Assert.Equal(employee.EmployeeReference.Value, stored["EmployeeReference"].AsString);
        Assert.Equal(employee.HomeAddress.Type.Value, stored["HomeAddress"]["Type"].AsString);
        Assert.Equal(employee.EqualOpportunities!.Id.Value,
            stored["EqualOpportunities"]["_id"].AsBsonBinaryData.ToGuid());
        Assert.Equal(stored, mapper.Capture(restored));
    }

    [Fact]
    public void GeneratedEmploymentStorageRoundTripsMembersAndCustomRawIdCodecs()
    {
        Employment employment = new EmploymentsTestFeature()
            .GenerateUniqueAggregate(EntityId<Employment>.New()).AggregateRoot;
        MongoDbAggregateRecordMapper<Employment> mapper = new(SampleDatabase.MongoDatabaseConfiguration);

        BsonDocument stored = mapper.Capture(employment);
        Employment restored = mapper.Restore(stored);

        Assert.Equal(employment.Id, restored.Id);
        Assert.Equal(employment.EmployeeId.Value, stored["EmployeeId"].AsBsonBinaryData.ToGuid());
        Assert.Equal(employment.Contracts[0].Id.Value,
            stored["Contracts"][0]["_id"].AsBsonBinaryData.ToGuid());
        Assert.Equal(employment.Contracts[0].EmployerNoticePeriod.Units.Value,
            stored["Contracts"][0]["EmployerNoticePeriod"]["Units"].AsInt32);
        Assert.Equal(stored, mapper.Capture(restored));
    }

    private static DateTime ToBsonDateTime(DateTime value) =>
        new(value.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond, value.Kind);
}
