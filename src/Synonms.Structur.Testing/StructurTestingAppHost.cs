using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Synonms.Structur.Testing;

public class StructurTestingAppHost(StructurTestingOptions options, StructurTestFixture.AppHostPrerequisites prerequisites) : DistributedApplicationFactory(options.EntryPoint)
{
    public DistributedApplication? Application { get; private set; }

    protected override void OnBuilderCreating(DistributedApplicationOptions applicationOptions, HostApplicationBuilderSettings hostOptions)
    {
        hostOptions.Configuration ??= new ConfigurationManager();
        hostOptions.Configuration["environment"] = options.Environment;
        
        options.AppHost.Configure(prerequisites, hostOptions.Configuration);
        
        applicationOptions.DisableDashboard = true;
    }
    
    protected override void OnBuilderCreated(DistributedApplicationBuilder applicationBuilder)
    {
        applicationBuilder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            options.HttpClient.ConfigureBuilder(clientBuilder);
        });
        
        applicationBuilder.Services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddFilter(applicationBuilder.Environment.ApplicationName, LogLevel.Debug);
            loggingBuilder.AddProvider(new TestContextLoggerProvider());
            
            options.Logging.Configure(loggingBuilder);
        });
    }
    
    protected override void OnBuilt(DistributedApplication application)
    {
        Application = application;
    }
    
    private sealed class TestContextLoggerProvider : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new TestContextLogger(categoryName);
        public void Dispose() { }
    }

    private sealed class TestContextLogger(string categoryName) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            string message = $"[{logLevel}] {categoryName}: {formatter(state, exception)}";

            if (exception is not null)
            {
                message += $"\n{exception}";
            }

            TestContext.Current.SendDiagnosticMessage(message);
        }
    }
}