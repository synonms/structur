using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employments;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public static class SampleDatabase
{
    public const string DatabaseName = "structur-sample";

    public static class Collections
    {
        public const string Employees = "employees";
        public const string Employments = "employments";
    }
    
    public static readonly MongoDatabaseConfiguration MongoDatabaseConfiguration = new(DatabaseName, new Dictionary<Type, string>
    {
        {typeof(Employee), Collections.Employees},
        {typeof(Employment), Collections.Employments},
    });
}