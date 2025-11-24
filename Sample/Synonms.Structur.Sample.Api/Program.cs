using System.Reflection;
using Serilog;
using Serilog.Extensions.Logging;
using Synonms.Structur.Infrastructure.MongoDb;
using Synonms.Structur.Sample.Api;
using Synonms.Structur.Sample.Api.Data;
using Synonms.Structur.Sample.Api.Features.Widgets.Domain;
using Synonms.Structur.WebApi.Controllers;
using Synonms.Structur.WebApi.DependencyInjection;
using Synonms.Structur.WebApi.Hosting;
using Synonms.Structur.WebApi.Hypermedia.Default;
using Synonms.Structur.WebApi.Hypermedia.Ion;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);
bool isGeneratingOpenApiFile = Assembly.GetEntryAssembly()?.GetName().Name == "GetDocument.Insider";

if (!isGeneratingOpenApiFile)
{
    webApplicationBuilder.AddServiceDefaults();
}

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(webApplicationBuilder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "FourFour")
    .CreateBootstrapLogger();

ILoggerFactory loggerFactory = new SerilogLoggerFactory(Log.Logger);

StructurOptions structurOptions = new()
{
    Assemblies = [SampleApiProject.Assembly],
    CorsConfiguration = corsPolicyBuilder =>
    {
        corsPolicyBuilder.WithOrigins("https://localhost:5003", "https://localhost:6003")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .Build();
    },
    MvcOptionsConfigurationAction = mvcOptions => mvcOptions.ClearFormatters().WithDefaultFormatters(loggerFactory).WithIonFormatters(loggerFactory),
    SwaggerUiConfigurationAction = swaggerUiOptions => swaggerUiOptions.SwaggerEndpoint("/swagger/v1.0/swagger.json", "v1.0")
};

webApplicationBuilder.Services.AddStructur(structurOptions);

Dictionary<Type, string> collectionNamesByAggregateType = new()
{
    {typeof(Widget), "widgets"}
};
MongoDatabaseConfiguration mongoDatabaseConfiguration = new("synonms-structur-sample-mongodb", collectionNamesByAggregateType);

if (!isGeneratingOpenApiFile)
{
    webApplicationBuilder.AddStructurMongoDb(mongoDatabaseConfiguration, "synonms-structur-sample-mongodb", SampleApiProject.Assembly);
}

WebApplication app = webApplicationBuilder.Build();

app.UseStructur(structurOptions);


if (app.Environment.IsDevelopment())
{
    DataSeeder dataSeeder = new();
    await dataSeeder.SeedDevelopmentDataAsync(app);
}

app.Run();
