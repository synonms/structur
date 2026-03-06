using MongoDB.Driver;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Infrastructure.MongoDb;
using Synonms.Structur.Sample.Api.Features.Employees.Domain;
using Synonms.Structur.Sample.Api.Features.Employees.Domain.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;

namespace Synonms.Structur.Sample.Api.Data;

public class DataSeeder
{
    private readonly Guid _lakersTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _spursTenantId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    
    private IMongoCollection<SampleTenant>? _tenantsCollection;
    private IMongoCollection<SampleProduct>? _productsCollection;
    private IMongoCollection<SampleUser>? _usersCollection;
    private IMongoCollection<DomainEvent>? _domainEventsCollection;
    private IMongoCollection<Employee>? _individualsCollection;

    public async Task SeedDevelopmentDataAsync(WebApplication webApplication, bool clearData = true)
    {
        await using AsyncServiceScope serviceScope = webApplication.Services.CreateAsyncScope();
        IMongoClient mongoClient = serviceScope.ServiceProvider.GetRequiredService<IMongoClient>();
        IUserActionProvider userActionProvider = serviceScope.ServiceProvider.GetRequiredService<IUserActionProvider>();
        
        SetCollections(mongoClient);

        if (clearData)
        {
            await ClearDataAsync();
        }

        await SeedTenantsAsync();
        await SeedProductsAsync();
        await SeedUsersAsync();
        
        await SeedIndividualsAsync(userActionProvider);
    }

    private void SetCollections(IMongoClient mongoClient)
    {
        IMongoDatabase database = mongoClient.GetDatabase(SampleDatabase.DatabaseName);
        
        _tenantsCollection ??= database.GetCollection<SampleTenant>(MongoDbConstants.Database.Collections.Tenants);
        _productsCollection ??= database.GetCollection<SampleProduct>(MongoDbConstants.Database.Collections.Products);
        _usersCollection ??= database.GetCollection<SampleUser>(MongoDbConstants.Database.Collections.Users);
        _domainEventsCollection ??= database.GetCollection<DomainEvent>(MongoDbConstants.Database.Collections.DomainEvents);
        _individualsCollection ??= database.GetCollection<Employee>(SampleDatabase.Collections.Employees);
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
        SampleTenant lakers = new()
        {
            Id = _lakersTenantId,
            Name = "Los Angeles Lakers"
        };
        
        SampleTenant spurs = new()
        {
            Id = _spursTenantId,
            Name = "Tottenham Hotspur"
        };

        await CreateTenant(lakers);
        await CreateTenant(spurs);
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
        SampleProduct product1 = new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000001"),
            Name = "Sample Product A"
        };
        
        SampleProduct product2 = new()
        {
            Id = Guid.Parse("20000000-0000-0000-0000-000000000002"),
            Name = "Sample Product B"
        };

        await CreateProduct(product1);
        await CreateProduct(product2);       
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
        SampleUser user1 = new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
            Name = "Sample User A"
        };
        
        SampleUser user2 = new()
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
            Name = "Sample User B"
        };

        await CreateUser(user1);
        await CreateUser(user2);
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
    
    private async Task SeedIndividualsAsync(IUserActionProvider userActionProvider)
    {
        EmployeeResource lebronResource = new(Guid.Parse("a9617306-ffa6-4355-9461-9dfcd6b702d4"), Link.EmptyLink())
        {
            EmployeeReference = "REF0001",
            NationalInsuranceNumber = "AA000001A",
            Title = TitleEnumeration.Mr,
            Forename = "Lebron",
            MiddleNames = null,
            Surname = "James",
            KnownAs = "LBJ",
            WorkPermitRequired = false,
            WorkPermitValidUntil = null,
            Notes = null,
            HomeAddress = new AddressValueObjectResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "Crypto.com Arena",
                Postcode = "LA1 1LA"
            },
            EmailContacts = 
            [
                new EmailContactValueObjectResource
                {
                    Address = "l.james@lakers.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource()
            {
                Id = Guid.NewGuid(),
                BirthDate = new DateOnly(1986, 01, 01),
                Sex = SexEnumeration.Male
            }
        };
        EmployeeResource lukaResource = new(Guid.Parse("294af0a0-0050-4562-8301-8a059bffefba"), Link.EmptyLink())
        {
            EmployeeReference = "REF0002",
            NationalInsuranceNumber = "AA000002A",
            Title = TitleEnumeration.Mr,
            Forename = "Luka",
            MiddleNames = null,
            Surname = "Doncic",
            KnownAs = null,
            WorkPermitRequired = true,
            WorkPermitValidUntil = new DateOnly(2030, 01, 01),
            Notes = null,
            HomeAddress = new AddressValueObjectResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "Crypto.com Arena",
                Postcode = "LA1 1LA"
            },
            EmailContacts = [
                new EmailContactValueObjectResource
                {
                    Address = "l.doncic@lakers.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource()
            {
                Id = Guid.NewGuid(),
                BirthDate = new DateOnly(1996, 01, 01),
                Sex = SexEnumeration.Male
            }
        };
        
        EmployeeCreatedEvent lebronCreatedEvent = new(userActionProvider, (EntityId<Employee>)lebronResource.Id, lebronResource, _lakersTenantId);
        EmployeeCreatedEvent lukaCreatedEvent = new(userActionProvider, (EntityId<Employee>)lukaResource.Id, lukaResource, _lakersTenantId);
        
        await CreateIndividualAsync(lebronCreatedEvent);
        await CreateIndividualAsync(lukaCreatedEvent);
        
        EmployeeResource glennResource = new(Guid.Parse("02f4ee29-fc72-42bc-9700-f1981e355e9d"), Link.EmptyLink())
        {
            EmployeeReference = "THFC0001",
            NationalInsuranceNumber = "AA000003A",
            Title = TitleEnumeration.Mr,
            Forename = "Glenn",
            MiddleNames = null,
            Surname = "Hoddle",
            KnownAs = null,
            WorkPermitRequired = false,
            WorkPermitValidUntil = null,
            Notes = null,
            HomeAddress = new AddressValueObjectResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "782 High Road",
                Postcode = "N17 0BX"
            },
            EmailContacts = 
            [
                new EmailContactValueObjectResource
                {
                    Address = "g.hoddle@thfc.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource()
            {
                Id = Guid.NewGuid(),
                BirthDate = new DateOnly(1966, 01, 01),
                Sex = SexEnumeration.Male
            }
        };
        EmployeeResource davidResource = new(Guid.Parse("c169cb00-c392-45df-a4a6-9caf25d9a9df"), Link.EmptyLink())
        {
            EmployeeReference = "THFC0002",
            NationalInsuranceNumber = "AA000004A",
            Title = TitleEnumeration.Mr,
            Forename = "David",
            MiddleNames = null,
            Surname = "Ginola",
            KnownAs = null,
            WorkPermitRequired = false,
            WorkPermitValidUntil = null,
            Notes = null,
            HomeAddress = new AddressValueObjectResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "782 High Road",
                Postcode = "N17 0BX"
            },
            EmailContacts = [
                new EmailContactValueObjectResource
                {
                    Address = "d.ginola@thfc.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource()
            {
                Id = Guid.NewGuid(),
                BirthDate = new DateOnly(1986, 01, 01),
                Sex = SexEnumeration.Male
            }
        };
        
        EmployeeCreatedEvent glennCreatedEvent = new(userActionProvider, (EntityId<Employee>)glennResource.Id, glennResource, _spursTenantId);
        EmployeeCreatedEvent davidCreatedEvent = new(userActionProvider, (EntityId<Employee>)davidResource.Id, davidResource, _spursTenantId);
        
        await CreateIndividualAsync(glennCreatedEvent);
        await CreateIndividualAsync(davidCreatedEvent);
    }

    private async Task CreateIndividualAsync(EmployeeCreatedEvent createdEvent)
    {
        Result<Employee> createdResult = await createdEvent.ApplyAsync(null);
            
        await createdResult.MatchAsync(
            async createdIndividual =>
            {
                Employee? existingIndividual = await _individualsCollection
                    .Find(x => x.Id == createdEvent.AggregateId)
                    .FirstOrDefaultAsync(CancellationToken.None);

                if (existingIndividual is null && _domainEventsCollection is not null && _individualsCollection is not null)
                {
                    await _domainEventsCollection.InsertOneAsync(createdEvent);
                    await _individualsCollection.InsertOneAsync(createdIndividual);
                }
            },
            errors => throw new ApplicationException($"Unable to create Employee Id '{createdEvent.AggregateId}': {errors}"));
    }
}
