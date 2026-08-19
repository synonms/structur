using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Http;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;
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
        JsonSerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { 
                new DateOnlyJsonConverter(),
                new OptionalDateOnlyJsonConverter(),
                new TimeOnlyJsonConverter(),
                new OptionalTimeOnlyJsonConverter(),
                new DefaultCustomJsonConverterFactory(new Version()),
                new DefaultLinkJsonConverter(),
                new DefaultFormDocumentJsonConverter(),
                new DefaultFormFieldJsonConverter(),
                new DefaultPaginationJsonConverter(),
                new DefaultErrorCollectionDocumentJsonConverter(),
                new DefaultErrorJsonConverter()
            }
        },
        HttpClient = new StructurTestingOptions.HttpClientOptions
        {
            ApiResourceName = Resources.Api,
            ApiEndpointName = null,
            ConfigureBuilder = httpClientBuilder => { },
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