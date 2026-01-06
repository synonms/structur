using MongoDB.Driver;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Infrastructure.MongoDb;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Infrastructure;

namespace Synonms.Structur.Sample.Tests.Integration;

public class TestDataSeeder
{
    private IMongoCollection<SampleTenant>? _tenantsCollection;
    private IMongoCollection<SampleProduct>? _productsCollection;
    private IMongoCollection<SampleUser>? _usersCollection;
    private IMongoCollection<DomainEvent>? _domainEventsCollection;
    private IMongoCollection<Individual>? _individualsCollection;

    public async Task SeedIntegrationTestDataAsync(IMongoClient mongoClient, bool clearData = true)
    {
        SetCollections(mongoClient);

        if (clearData)
        {
            await ClearDataAsync();
        }

        await SeedTenantsAsync();
        await SeedProductsAsync();
        await SeedUsersAsync();
    }
    
    private void SetCollections(IMongoClient mongoClient)
    {
        IMongoDatabase database = mongoClient.GetDatabase(SampleDatabase.DatabaseName);
        
        _tenantsCollection ??= database.GetCollection<SampleTenant>(MongoDbConstants.Database.Collections.Tenants);
        _productsCollection ??= database.GetCollection<SampleProduct>(MongoDbConstants.Database.Collections.Products);
        _usersCollection ??= database.GetCollection<SampleUser>(MongoDbConstants.Database.Collections.Users);
        _domainEventsCollection ??= database.GetCollection<DomainEvent>(MongoDbConstants.Database.Collections.DomainEvents);
        _individualsCollection ??= database.GetCollection<Individual>("individuals");
    }

    private async Task ClearDataAsync()
    {
        await _tenantsCollection.DeleteManyAsync(x => true);
        await _productsCollection.DeleteManyAsync(x => true);
        await _usersCollection.DeleteManyAsync(x => true);
        await _domainEventsCollection.DeleteManyAsync(x => true);
        await _individualsCollection.DeleteManyAsync(x => true);
    }
    
    private async Task SeedTenantsAsync()
    {
        SampleTenant testTenant = new()
        {
            Id = TestTenant.Id,
            Name = TestTenant.Name
        };

        await CreateTenant(testTenant);
    }

    private async Task CreateTenant(SampleTenant tenant)
    {
        SampleTenant? existingTenant = await _tenantsCollection
            .Find(x => x.Id == tenant.Id)
            .FirstOrDefaultAsync(CancellationToken.None);

        if (existingTenant is null)
        {
            await _tenantsCollection!.InsertOneAsync(tenant);
        }
    }
    
    private async Task SeedProductsAsync()
    {
        SampleProduct testProduct = new()
        {
            Id = TestProduct.Id,
            Name = TestProduct.Name
        };

        await CreateProduct(testProduct);
    }
    
    private async Task CreateProduct(SampleProduct product)
    {
        SampleProduct? existingProduct = await _productsCollection
            .Find(x => x.Id == product.Id)
            .FirstOrDefaultAsync(CancellationToken.None);

        if (existingProduct is null)
        {
            await _productsCollection!.InsertOneAsync(product);
        }
    }

    private async Task SeedUsersAsync()
    {
        SampleUser testUser = new()
        {
            Id = TestUser.Id,
            Name = TestUser.Name
        };
        
        await CreateUser(testUser);
    }
    
    private async Task CreateUser(SampleUser user)
    {
        SampleUser? existingUser = await _usersCollection
            .Find(x => x.Id == user.Id)
            .FirstOrDefaultAsync(CancellationToken.None);

        if (existingUser is null)
        {
            await _usersCollection!.InsertOneAsync(user);
        }
    }
}