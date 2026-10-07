using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Infrastructure.Persistence;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employees.Persistence;
using Synonms.Structur.Sample.Api.Features.Employments;
using Synonms.Structur.Sample.Api.Features.Employments.Persistence;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public static class SampleDatabase
{
    public const string DatabaseName = "structur-sample";

    public static class Collections
    {
        public const string Employees = "employees";
        public const string Employments = "employments";
    }
    
    public static readonly MongoDatabaseConfiguration MongoDatabaseConfiguration = new(DatabaseName, new Dictionary<Type, AggregatePersistenceConfiguration>
    {
        {typeof(Employee), new AggregatePersistenceConfiguration(Collections.Employees, typeof(EmployeeRecord))},
        {typeof(Employment), new AggregatePersistenceConfiguration(Collections.Employments, typeof(EmploymentRecord))},
    });
}