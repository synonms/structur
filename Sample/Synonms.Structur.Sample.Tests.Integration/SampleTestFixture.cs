using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Server.Hypermedia.Default;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Sample.Api;
using Synonms.Structur.Sample.Api.Infrastructure;
using Synonms.Structur.Sample.AppHost;
using Synonms.Structur.Testing;

[assembly: AssemblyFixture(typeof(Synonms.Structur.Sample.Tests.Integration.SampleTestFixture))]

namespace Synonms.Structur.Sample.Tests.Integration;

public class SampleTestFixture() : StructurTestFixture( 
    new StructurTestingOptions
    {
        EntryPoint = typeof(SampleAppHostProject),
        Environment = "IntegrationTest",
        MediaType = MediaTypes.Json,
        JsonSerializerOptions = DefaultOutputFormatter.JsonSerializerOptions,
        HttpClient = new StructurTestingOptions.HttpClientOptions
        {
            ApiResourceName = Resources.Api,
            ApiEndpointName = null,
            ConfigureBuilder = httpClientBuilder =>
            {
                httpClientBuilder.AddStandardResilienceHandler();
            },
            ConfigureClient = httpClient =>
            {
                httpClient.DefaultRequestHeaders.Add("Accept", MediaTypes.Json);
                httpClient.DefaultRequestHeaders.Add(HttpHeaders.TenantId, TestTenant.Id.ToString());
                httpClient.DefaultRequestHeaders.Add(HttpHeaders.ProductId, TestProduct.Id.ToString());
            }
        },
        AppHost = new StructurTestingOptions.AppHostOptions
        {
            WaitUntilHealthyForResourceNames = [Resources.Api]
        },
        TestHost = new StructurTestingOptions.TestHostOptions
        {
            Configure = async (distributedApplication, hostApplicationBuilder) =>
            {
                string? sampleDatabaseConnectionString = await distributedApplication.GetConnectionStringAsync(Resources.MongoDatabase, TestContext.Current.CancellationToken);

                hostApplicationBuilder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:" + Resources.MongoDatabase, sampleDatabaseConnectionString }
                });
                
                hostApplicationBuilder.AddStructurMongoDb<SampleTenant>(SampleDatabase.MongoDatabaseConfiguration, Resources.MongoDatabase, SampleApiProject.Assembly);
            }
        }
    })
{
    protected override async Task SeedDataAsync()
    {
        using IServiceScope scope = ServiceScopeFactory.CreateScope();
        IMongoClient mongoClient = scope.ServiceProvider.GetRequiredService<IMongoClient>();
        
        await new TestDataSeeder().SeedIntegrationTestDataAsync(mongoClient, clearData: true);
    }
}