using MongoDB.Driver;
using Synonms.Structur.Application.Lookups;
using Synonms.Structur.Application.Schema.Forms;
using Synonms.Structur.Domain.Lookups;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDbLookupOptionsProvider : ILookupOptionsProvider
{
    private readonly IMongoCollection<Lookup> _mongoCollection;

    public MongoDbLookupOptionsProvider(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<Lookup>(MongoDbConstants.Database.Collections.Lookups);
    }

    public IEnumerable<FormFieldOption> Get(string discriminator)
    {
        List<Lookup> lookups = _mongoCollection
            .Find(x => x.Discriminator == discriminator)
            .ToList();

        return lookups.Select(lookup => new FormFieldOption(lookup.Id.Value.ToString())
        {
            Label = lookup.LookupName,
            IsEnabled = !lookup.IsDeleted
        });
    }
}