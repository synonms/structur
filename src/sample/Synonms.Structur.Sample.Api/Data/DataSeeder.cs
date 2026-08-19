using MongoDB.Driver;
using Synonms.Structur.Api.Core.Schema;
using Synonms.Structur.Api.Core.ValueObjects;
using Synonms.Structur.Api.Core.ValueObjects.Enumerations;
using Synonms.Structur.Api.Server.Users;
using Synonms.Structur.Api.Server.Versioning.Context;
using Synonms.Structur.Core.Entities;
using Synonms.Structur.Core.Functional;
using Synonms.Structur.Domain.Events;
using Synonms.Structur.Domain.ValueObjects;
using Synonms.Structur.Infrastructure.MongoDb;
using Synonms.Structur.Sample.Api.Features.Employees;
using Synonms.Structur.Sample.Api.Features.Employees.Events;
using Synonms.Structur.Sample.Api.Features.Employments;
using Synonms.Structur.Sample.Api.Features.Employments.Events;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.ClientApi.Features.Employees;
using Synonms.Structur.Sample.ClientApi.Features.Employments;
using EmailContactTypeEnumeration = Synonms.Structur.Api.Core.ValueObjects.Enumerations.EmailContactTypeEnumeration;
using SexEnumeration = Synonms.Structur.Api.Core.ValueObjects.Enumerations.SexEnumeration;
using TitleEnumeration = Synonms.Structur.Api.Core.ValueObjects.Enumerations.TitleEnumeration;

namespace Synonms.Structur.Sample.Api.Data;

public class DataSeeder
{
    private readonly Guid _lakersTenantId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    private readonly Guid _spursTenantId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    
    private IMongoCollection<SampleTenant>? _tenantsCollection;
    private IMongoCollection<SampleProduct>? _productsCollection;
    private IMongoCollection<SampleUser>? _usersCollection;
    private IMongoCollection<DomainEvent>? _domainEventsCollection;
    private IMongoCollection<Employee>? _employeesCollection;
    private IMongoCollection<Employment>? _employmentsCollection;

    public async Task SeedDevelopmentDataAsync(WebApplication webApplication, bool clearData = true)
    {
        await using AsyncServiceScope serviceScope = webApplication.Services.CreateAsyncScope();
        IMongoClient mongoClient = serviceScope.ServiceProvider.GetRequiredService<IMongoClient>();
        IUserActionProvider userActionProvider = serviceScope.ServiceProvider.GetRequiredService<IUserActionProvider>();
        IVersionContext versionContext = serviceScope.ServiceProvider.GetRequiredService<IVersionContext>();
        
        SetCollections(mongoClient);

        if (clearData)
        {
            await ClearDataAsync();
        }

        await SeedTenantsAsync();
        await SeedProductsAsync();
        await SeedUsersAsync();
        
        await SeedEmployeesAsync(userActionProvider, versionContext);
        await SeedEmploymentsAsync(userActionProvider, versionContext);
    }

    private void SetCollections(IMongoClient mongoClient)
    {
        IMongoDatabase database = mongoClient.GetDatabase(SampleDatabase.DatabaseName);
        
        _tenantsCollection ??= database.GetCollection<SampleTenant>(MongoDbConstants.Database.Collections.Tenants);
        _productsCollection ??= database.GetCollection<SampleProduct>(MongoDbConstants.Database.Collections.Products);
        _usersCollection ??= database.GetCollection<SampleUser>(MongoDbConstants.Database.Collections.Users);
        _domainEventsCollection ??= database.GetCollection<DomainEvent>(MongoDbConstants.Database.Collections.DomainEvents);
        _employeesCollection ??= database.GetCollection<Employee>(SampleDatabase.Collections.Employees);
        _employmentsCollection ??= database.GetCollection<Employment>(SampleDatabase.Collections.Employments);
    }

    private async Task ClearDataAsync()
    {
        await _tenantsCollection.DeleteManyAsync(x => true);
        await _productsCollection.DeleteManyAsync(x => true);
        await _usersCollection.DeleteManyAsync(x => true);
        await _domainEventsCollection.DeleteManyAsync(x => true);
        await _employeesCollection.DeleteManyAsync(x => true);
        await _employmentsCollection.DeleteManyAsync(x => true);
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
    
    private async Task SeedEmployeesAsync(IUserActionProvider userActionProvider, IVersionContext versionContext)
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
            HomeAddress = new AddressResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "Crypto.com Arena",
                Postcode = "LA1 1LA"
            },
            EmailContacts = 
            [
                new EmailContactResource
                {
                    Type = EmailContactTypeEnumeration.Company,
                    Address = "l.james@lakers.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource
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
            HomeAddress = new AddressResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "Crypto.com Arena",
                Postcode = "LA1 1LA"
            },
            EmailContacts = [
                new EmailContactResource
                {
                    Type = EmailContactTypeEnumeration.Company,
                    Address = "l.doncic@lakers.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource
            {
                Id = Guid.NewGuid(),
                BirthDate = new DateOnly(1996, 01, 01),
                Sex = SexEnumeration.Male
            }
        };
        
        EmployeeCreatedEvent lebronCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employee>)lebronResource.Id, lebronResource, _lakersTenantId);
        EmployeeCreatedEvent lukaCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employee>)lukaResource.Id, lukaResource, _lakersTenantId);
        
        await CreateEmployeeAsync(lebronCreatedEvent);
        await CreateEmployeeAsync(lukaCreatedEvent);
        
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
            HomeAddress = new AddressResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "782 High Road",
                Postcode = "N17 0BX"
            },
            EmailContacts = 
            [
                new EmailContactResource
                {
                    Type = EmailContactTypeEnumeration.Company,
                    Address = "g.hoddle@thfc.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource
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
            HomeAddress = new AddressResource
            { 
                Type = AddressTypeEnumeration.Home,
                Line1 = "782 High Road",
                Postcode = "N17 0BX"
            },
            EmailContacts = [
                new EmailContactResource
                {
                    Type = EmailContactTypeEnumeration.Company,
                    Address = "d.ginola@thfc.com",
                    IsPrimary = true
                }
            ],
            TelephoneContacts = [],
            EqualOpportunities = new EmployeeEqualOpportunitiesResource
            {
                Id = Guid.NewGuid(),
                BirthDate = new DateOnly(1986, 01, 01),
                Sex = SexEnumeration.Male
            }
        };
        
        EmployeeCreatedEvent glennCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employee>)glennResource.Id, glennResource, _spursTenantId);
        EmployeeCreatedEvent davidCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employee>)davidResource.Id, davidResource, _spursTenantId);
        
        await CreateEmployeeAsync(glennCreatedEvent);
        await CreateEmployeeAsync(davidCreatedEvent);
    }

    private async Task CreateEmployeeAsync(EmployeeCreatedEvent createdEvent)
    {
        Result<Employee> createdResult = await createdEvent.ApplyAsync(null);
            
        await createdResult.MatchAsync(
            async createdEmployee =>
            {
                Employee? existingEmployee = await _employeesCollection
                    .Find(x => x.Id == createdEvent.AggregateId)
                    .FirstOrDefaultAsync(CancellationToken.None);

                if (existingEmployee is null && _domainEventsCollection is not null && _employeesCollection is not null)
                {
                    await _domainEventsCollection.InsertOneAsync(createdEvent);
                    await _employeesCollection.InsertOneAsync(createdEmployee);
                }
            },
            errors => throw new ApplicationException($"Unable to create Employee Id '{createdEvent.AggregateId}': {errors}"));
    }
    
    private async Task SeedEmploymentsAsync(IUserActionProvider userActionProvider, IVersionContext versionContext)
    {
        EmploymentResource lebronResource = new(Guid.Parse("517625f8-eaae-4095-b9ce-41194341acf8"), Link.EmptyLink())
        {
            EmployeeId = Guid.Parse("a9617306-ffa6-4355-9461-9dfcd6b702d4"),
            EmploymentReference = "LAL0001",
            ContinuousStartDate = new DateOnly(2020, 08, 01),
            Contracts = 
            [
                new EmploymentContractResource
                {
                    StartDate = new DateOnly(2020, 08, 01),
                    ProbationEndDate = null,
                    EndDate = new DateOnly(2022, 07, 31),
                    EmployerNoticePeriod = new PeriodResource
                    {
                        Units = 3, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    EmployeeNoticePeriod = new PeriodResource
                    {
                        Units = 1, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    Position = "Small Forward",
                    Location = WorkLocation.From(WorkLocationEnumeration.Roaming),
                    LocationNotes = null,
                    ReportsToEmployeeId = null,
                    CarRegistrationPlate = null,
                    Notes = null,
                    CanClaimTravelExpensesToOffice = false
                },
                new EmploymentContractResource
                {
                    StartDate = new DateOnly(2022, 08, 01),
                    ProbationEndDate = null,
                    EndDate = null,
                    EmployerNoticePeriod = new PeriodResource
                    {
                        Units = 0, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    EmployeeNoticePeriod = new PeriodResource
                    {
                        Units = 0, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    Position = "Small Forward",
                    Location = WorkLocation.From(WorkLocationEnumeration.Roaming),
                    LocationNotes = null,
                    ReportsToEmployeeId = null,
                    CarRegistrationPlate = null,
                    Notes = null,
                    CanClaimTravelExpensesToOffice = false
                }
            ],
            BankDetails = new UkBankDetailsResource
            {
                BankName = "Bank of America",
                SortCode = "123456",
                AccountNumber = "12345678",
                AccountName = "LeBron James",
                BuildingSocietyRollNumber = null
            }
        };
        EmploymentResource lukaResource = new(Guid.Parse("21346a61-67d4-410e-bf83-ae96a107fef1"), Link.EmptyLink())
        {
            EmployeeId = Guid.Parse("294af0a0-0050-4562-8301-8a059bffefba"),
            EmploymentReference = "LAL0002",
            ContinuousStartDate = new DateOnly(2024, 08, 01),
            Contracts = 
            [
                new EmploymentContractResource
                {
                    StartDate = new DateOnly(2024, 08, 01),
                    ProbationEndDate = null,
                    EndDate = null,
                    EmployerNoticePeriod = new PeriodResource
                    {
                        Units = 3, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    EmployeeNoticePeriod = new PeriodResource
                    {
                        Units = 1, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    Position = "Point Guard",
                    Location = WorkLocation.From(WorkLocationEnumeration.Roaming),
                    LocationNotes = null,
                    ReportsToEmployeeId = null,
                    CarRegistrationPlate = null,
                    Notes = null,
                    CanClaimTravelExpensesToOffice = false
                }
            ],
            BankDetails = new UkBankDetailsResource
            {
                BankName = "Bank of America",
                SortCode = "123456",
                AccountNumber = "23456789",
                AccountName = "Luka Doncic",
                BuildingSocietyRollNumber = null
            }
        };
        
        EmploymentCreatedEvent lebronCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employment>)lebronResource.Id, lebronResource, _lakersTenantId);
        EmploymentCreatedEvent lukaCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employment>)lukaResource.Id, lukaResource, _lakersTenantId);
        
        await CreateEmploymentAsync(lebronCreatedEvent);
        await CreateEmploymentAsync(lukaCreatedEvent);
        
        EmploymentResource glennResource = new(Guid.Parse("fac6ca58-ea30-4f25-a105-46b89ca6499e"), Link.EmptyLink())
        {            
            EmployeeId = Guid.Parse("02f4ee29-fc72-42bc-9700-f1981e355e9d"),
            EmploymentReference = "THFC0001",
            ContinuousStartDate = new DateOnly(1980, 07, 01),
            Contracts = 
            [
                new EmploymentContractResource
                {
                    StartDate = new DateOnly(1980, 07, 01),
                    ProbationEndDate = null,
                    EndDate = new DateOnly(1990, 05, 31),
                    EmployerNoticePeriod = new PeriodResource
                    {
                        Units = 3, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    EmployeeNoticePeriod = new PeriodResource
                    {
                        Units = 1, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    Position = "Midfielder",
                    Location = WorkLocation.From(WorkLocationEnumeration.Roaming),
                    LocationNotes = null,
                    ReportsToEmployeeId = null,
                    CarRegistrationPlate = null,
                    Notes = null,
                    CanClaimTravelExpensesToOffice = false
                }
            ],
            BankDetails = new UkBankDetailsResource
            {
                BankName = "Barclays",
                SortCode = "123456",
                AccountNumber = "12345678",
                AccountName = "Glenn Hoddle",
                BuildingSocietyRollNumber = null
            }
        };
        EmploymentResource davidResource = new(Guid.Parse("fe48ce79-84f2-49f9-9de8-db6f958ad7a5"), Link.EmptyLink())
        {
            EmployeeId = Guid.Parse("c169cb00-c392-45df-a4a6-9caf25d9a9df"),
            EmploymentReference = "THFC0002",
            ContinuousStartDate = new DateOnly(1996, 07, 01),
            Contracts = 
            [
                new EmploymentContractResource
                {
                    StartDate = new DateOnly(1996, 07, 01),
                    ProbationEndDate = null,
                    EndDate = new DateOnly(1998, 05, 31),
                    EmployerNoticePeriod = new PeriodResource
                    {
                        Units = 3, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    EmployeeNoticePeriod = new PeriodResource
                    {
                        Units = 1, 
                        Interval = Interval.From(IntervalEnumeration.Month)
                    },
                    Position = "Winger",
                    Location = WorkLocation.From(WorkLocationEnumeration.Roaming),
                    LocationNotes = null,
                    ReportsToEmployeeId = null,
                    CarRegistrationPlate = null,
                    Notes = null,
                    CanClaimTravelExpensesToOffice = false
                }
            ],
            BankDetails = new UkBankDetailsResource
            {
                BankName = "Barclays",
                SortCode = "123456",
                AccountNumber = "23456789",
                AccountName = "David Ginola",
                BuildingSocietyRollNumber = null
            }
        };
        
        EmploymentCreatedEvent glennCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employment>)glennResource.Id, glennResource, _spursTenantId);
        EmploymentCreatedEvent davidCreatedEvent = new(userActionProvider, versionContext, (EntityId<Employment>)davidResource.Id, davidResource, _spursTenantId);
        
        await CreateEmploymentAsync(glennCreatedEvent);
        await CreateEmploymentAsync(davidCreatedEvent);
    }
    
    private async Task CreateEmploymentAsync(EmploymentCreatedEvent createdEvent)
    {
        Result<Employment> createdResult = await createdEvent.ApplyAsync(null);
            
        await createdResult.MatchAsync(
            async createdEmployment =>
            {
                Employment? existingEmployment = await _employmentsCollection
                    .Find(x => x.Id == createdEvent.AggregateId)
                    .FirstOrDefaultAsync(CancellationToken.None);

                if (existingEmployment is null && _domainEventsCollection is not null && _employmentsCollection is not null)
                {
                    await _domainEventsCollection.InsertOneAsync(createdEvent);
                    await _employmentsCollection.InsertOneAsync(createdEmployment);
                }
            },
            errors => throw new ApplicationException($"Unable to create Employment Id '{createdEvent.AggregateId}': {errors}"));
    }
}
