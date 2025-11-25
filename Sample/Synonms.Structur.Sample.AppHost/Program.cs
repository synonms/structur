IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<MongoDBServerResource> mongo = builder.AddMongoDB("synonms-structur-sample-mongo")
        .WithMongoExpress()
        .WithLifetime(ContainerLifetime.Persistent)
        .WithDataVolume()
        .WithIconName("DatabaseStack");

IResourceBuilder<MongoDBDatabaseResource> mongodb = mongo.AddDatabase("synonms-structur-sample-mongodb")
    .WithIconName("Database");

IResourceBuilder<ProjectResource> api = builder.AddProject<Projects.Synonms_Structur_Sample_Api>("synonms-structur-sample-api")
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", builder.Environment.EnvironmentName)
    .WithReference(mongodb).WaitFor(mongodb);

builder.Build().Run();