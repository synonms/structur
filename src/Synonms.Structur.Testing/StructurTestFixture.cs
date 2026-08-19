using System.Text.Json;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WireMock.Net.Testcontainers;
using Xunit;

namespace Synonms.Structur.Testing;

public abstract class StructurTestFixture(StructurTestingOptions options) : IAsyncLifetime
{
    public class AppHostPrerequisites
    {
        public WireMockContainer? WireMockContainer { get; set; }
    }
    
    public string MediaType { get; } = options.MediaType;
    public JsonSerializerOptions? JsonSerializerOptions { get; } = options.JsonSerializerOptions;
    public HttpClient HttpClient { get; private set; } = null!;
    public IServiceScopeFactory ServiceScopeFactory { get; private set; } = null!;
    protected AppHostPrerequisites Prerequisites { get; } = new();
    protected StructurTestingAppHost? AppHost;
    protected ServiceProvider? ServiceProvider;
    
    public async ValueTask InitializeAsync()
    {
        TestContext.Current.TestOutputHelper?.WriteLine("StructurTestFixture.InitializeAsync");
        
        if (options.WireMock.IsRequired)
        {
            await SetupWireMockAsync(timeout: options.WireMock.Timeout);
        }

        await StartServicesAsync(options.AppHost.StartupTimeout);
        await WaitForHealthyResourcesAsync(options.AppHost.WaitUntilHealthyForResourceNames, options.AppHost.WaitUntilHealthyTimeout);
        await SetupServiceProvider();
        await SeedDataAsync();
    }
    
    public async ValueTask DisposeAsync()
    {
        if (AppHost is not null)
        {
            await AppHost.DisposeAsync();
        }

        if (ServiceProvider is not null)
        {
            await ServiceProvider.DisposeAsync();
        }
        
        if (Prerequisites.WireMockContainer is not null)
        {
            await Prerequisites.WireMockContainer.DisposeAsync();
        }
    }
    
    protected async Task SetupWireMockAsync(TimeSpan timeout)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout);

        Prerequisites.WireMockContainer = new WireMockContainerBuilder()
            .WithAutoRemove(false)      // Resource Reaper is disabled for podman wiremock/test containers
            .WithReadStaticMappings()
            .Build();

        try
        {
            await Prerequisites.WireMockContainer.StartAsync(cancellationTokenSource.Token);
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            throw new Exception("Failed to start WireMock container");
        }
    }
    
    protected async Task StartServicesAsync(TimeSpan timeout)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout);

        try
        {
            AppHost = new StructurTestingAppHost(options, Prerequisites);
            
            await AppHost.StartAsync(cancellationTokenSource.Token);
        }
        catch (Exception exception)
        {
            TestContext.Current.SendDiagnosticMessage("ERROR: Exception occurred starting Aspire AppHost: {0}", exception);
            throw new Exception("Failed to start Aspire AppHost", exception);
        }

        if (AppHost.Application is null)
        {
            TestContext.Current.SendDiagnosticMessage("ERROR: Aspire AppHost started but DistributedApplication is not initialised.");
            throw new NullReferenceException("DistributedApplication not initialised");
        }
    }

    protected virtual Task SeedDataAsync() => Task.CompletedTask;
    
    private async Task WaitForHealthyResourcesAsync(IEnumerable<string> resourceNames, TimeSpan timeout)
    {
        using CancellationTokenSource cancellationTokenSource = new(timeout);
        
        Task[] waitTasks = resourceNames.Select(resourceName => WaitForResourceAsync(resourceName, cancellationTokenSource.Token)).ToArray();

        await Task.WhenAll(waitTasks);
    }

    private async Task WaitForResourceAsync(string aspireResourceName, CancellationToken cancellationToken)
    {
        if (AppHost?.Application is null)
        {
            throw new NullReferenceException("AppHost Application not initialised");
        }

        try
        {
            await AppHost.Application.ResourceNotifications.WaitForResourceHealthyAsync(aspireResourceName, cancellationToken);

            TestContext.Current.SendDiagnosticMessage("SUCCESS: Resource '{0}' healthy", aspireResourceName);
        }
        catch (Exception exception)
        {
            TestContext.Current.SendDiagnosticMessage("ERROR: Exception occurred waiting for resource '{0}': {1}", aspireResourceName, exception);
            throw new Exception($"Failure waiting for resource '{aspireResourceName}'", exception);
        }
    }
    
    private async Task SetupServiceProvider()
    {
        if (AppHost?.Application is null)
        {
            throw new NullReferenceException("AppHost Application not initialised");
        }

        HostApplicationBuilderSettings settings = new()
        {
            EnvironmentName = options.Environment
        };
        HostApplicationBuilder host = new(settings);
        
        await options.TestHost.Configure.Invoke(AppHost.Application, host);

        ServiceProvider = host.Services.BuildServiceProvider();
        HttpClient = AppHost.Application.CreateHttpClient(options.HttpClient.ApiResourceName, options.HttpClient.ApiEndpointName);
        options.HttpClient.ConfigureClient(HttpClient);
        ServiceScopeFactory = ServiceProvider.GetRequiredService<IServiceScopeFactory>();
    }
}