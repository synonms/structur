using Serilog;
using Serilog.Extensions.Logging;
using Synonms.Structur.Sample.Api;
using Synonms.Structur.WebApi.Controllers;
using Synonms.Structur.WebApi.DependencyInjection;
using Synonms.Structur.WebApi.Hosting;
using Synonms.Structur.WebApi.Hypermedia.Default;
using Synonms.Structur.WebApi.Hypermedia.Ion;

WebApplicationBuilder webApplicationBuilder = WebApplication.CreateBuilder(args);

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

webApplicationBuilder.AddServiceDefaults();

WebApplication app = webApplicationBuilder.Build();

app.UseStructur(structurOptions);

app.Run();
