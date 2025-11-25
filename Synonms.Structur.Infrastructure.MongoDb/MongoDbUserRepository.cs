using MongoDB.Driver;
using Synonms.Structur.Application.Users;
using Synonms.Structur.Application.Users.Persistence;
using Synonms.Structur.Core.Functional;

namespace Synonms.Structur.Infrastructure.MongoDb;

public class MongoDbUserRepository<TUser> : IUserRepository<TUser>
    where TUser : StructurUser
{
    private readonly IMongoCollection<TUser> _mongoCollection;

    public MongoDbUserRepository(IMongoClient mongoClient, MongoDatabaseConfiguration mongoDatabaseConfiguration)
    {
        _mongoCollection = mongoClient.GetDatabase(mongoDatabaseConfiguration.DatabaseName)
            .GetCollection<TUser>(MongoDbConstants.Database.Collections.Tenants);
    }
    
    public async Task<Maybe<TUser>> FindAuthenticatedUserAsync(Guid id, CancellationToken cancellationToken)
    {
        TUser? user = await _mongoCollection
            .Find(x => x.Id == id)
            .FirstOrDefaultAsync(cancellationToken);

        return user;
    }
}