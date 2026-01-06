using Synonms.Structur.Sample.AppHost;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<MongoDBServerResource> mongo = builder.Environment.EnvironmentName == "IntegrationTest" 
    ? builder.AddMongoDB(Resources.MongoServerForTest)
    : builder.AddMongoDB(Resources.MongoServer)
        .WithMongoExpress()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithIconName("DatabaseStack");

IResourceBuilder<MongoDBDatabaseResource> mongodb = mongo.AddDatabase(Resources.MongoDatabase, "structur-sample")
    .WithIconName("Database");

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.Synonms_Structur_Sample_Api>(Resources.Api)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(mongodb).WaitFor(mongodb)
    .WithIconName("SettingsCogMultiple");

builder.AddProject<Projects.Synonms_Structur_Sample_Ui>(Resources.Ui)
    .WithReference(api).WaitFor(api)
    .WithIconName("ShareScreenPerson");

builder.Build().Run();