using System.Reflection;
using Serilog;
using Serilog.Extensions.Logging;
using Synonms.Structur.Infrastructure.MongoDb.Hosting;
using Synonms.Structur.Sample.Api;
using Synonms.Structur.Sample.Api.Data;
using Synonms.Structur.Sample.Api.Features.Individuals.Domain;
using Synonms.Structur.Sample.Api.Infrastructure;
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
    .Enrich.WithProperty("Application", "Structur Sample API")
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
    OpenApiDocumentPath = "/openapi/v1.json",
    SwaggerUiConfigurationAction = swaggerUiOptions =>
    {
        swaggerUiOptions.SwaggerEndpoint("/openapi/v1.json", "v1");
        swaggerUiOptions.DocumentTitle = "Structur Sample API";
    },
    UseEmptyLookups = true
};

webApplicationBuilder.Services.AddStructur<SampleUser, SampleProduct, SampleTenant>(structurOptions);

Dictionary<Type, string> collectionNamesByAggregateType = new()
{
    {typeof(Individual), "individuals"},
};
MongoDatabaseConfiguration mongoDatabaseConfiguration = new("synonms-structur-sample-mongodb", collectionNamesByAggregateType);

if (!isGeneratingOpenApiFile)
{
    webApplicationBuilder.AddStructurMongoDb<SampleTenant>(mongoDatabaseConfiguration, "synonms-structur-sample-mongodb", SampleApiProject.Assembly)
        .WithPipelineRepositories();
}

WebApplication app = webApplicationBuilder.Build();

app.UseStructur<SampleUser, SampleProduct, SampleTenant>(structurOptions);

if (app.Environment.IsDevelopment())
{
    DataSeeder dataSeeder = new();
    await dataSeeder.SeedDevelopmentDataAsync(app, clearData: true);
}

app.Run();
