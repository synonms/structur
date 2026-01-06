using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;

namespace Synonms.Structur.Sample.Api.Infrastructure;

public static class SampleDatabase
{
    public const string DatabaseName = "structur-sample";
    
    public static readonly MongoDatabaseConfiguration MongoDatabaseConfiguration = new(DatabaseName, new Dictionary<Type, string>
    {
        {typeof(Individual), "individuals"}
    });
}