using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Synonms.Structur.Api.Core.Content;
using Synonms.Structur.Api.Core.Serialisation;
using Synonms.Structur.Api.Core.Serialisation.Default;
using Synonms.Structur.Api.Server.Hypermedia.Default;
using WireMock.Net.Testcontainers;

namespace Synonms.Structur.Testing;

public class StructurTestingOptions
{
    public class AppHostOptions
    {
        public Action<StructurTestFixture.AppHostPrerequisites, ConfigurationManager> Configure { get; set; } = (prerequisites, configurationManager) => { };
        public TimeSpan StartupTimeout { get; set; } = TimeSpan.FromMinutes(5);
        public List<string> WaitUntilHealthyForResourceNames { get; set; } = [];
        public TimeSpan WaitUntilHealthyTimeout { get; set; } = TimeSpan.FromSeconds(60);
    }
    
    public class WireMockOptions
    {
        public bool IsRequired { get; set; } = false;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(300);
        public Func<WireMockContainer?, Dictionary<string, string?>>? AppHostConfiguration { get; set; } 
    }

    public class TestHostOptions
    {
        public Func<DistributedApplication, IHostApplicationBuilder, Task> Configure { get; set; } = (distributedApplication, hostApplicationBuilder) => Task.CompletedTask;
    }
    
    public class HttpClientOptions
    {
        public bool IsRequired { get; set; } = true;
        public required string ApiResourceName { get; set; }
        public string? ApiEndpointName { get; set; }
        public Action<IHttpClientBuilder> ConfigureBuilder { get; set; } = httpClientBuilder =>
        {
            httpClientBuilder.AddStandardResilienceHandler();
        };
        public Action<HttpClient> ConfigureClient { get; set; } = httpClient => {};
    }

    public class LoggingOptions
    {
        public Action<ILoggingBuilder> Configure { get; set; } = loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            loggingBuilder.AddFilter("Aspire.", LogLevel.Debug);
        };
    }
    
    public required Type EntryPoint { get; set; }
    
    public string Environment { get; set; } = "Test";

    public string MediaType { get; set; } = MediaTypes.Json;

    public JsonSerializerOptions? JsonSerializerOptions { get; set; } = new()
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
    };

    public AppHostOptions AppHost { get; set; } = new();
    
    public TestHostOptions TestHost { get; set; } = new();

    public required HttpClientOptions HttpClient { get; set; }

    public WireMockOptions WireMock { get; set; } = new();

    public LoggingOptions Logging { get; set; } = new();
}