using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Sample.Api.Features.Employees.Domain;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public static class SampleDatabase
{
    public const string DatabaseName = "structur-sample";

    public static class Collections
    {
        public const string Employees = "employees";
    }
    
    public static readonly MongoDatabaseConfiguration MongoDatabaseConfiguration = new(DatabaseName, new Dictionary<Type, string>
    {
        {typeof(Employee), Collections.Employees}
    });
}