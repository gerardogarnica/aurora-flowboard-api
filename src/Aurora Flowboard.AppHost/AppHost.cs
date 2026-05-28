IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresDatabaseResource> flowboardDatabase = builder
    .AddPostgres("database")
    .WithImage("postgres:17")
    .WithBindMount("../../.containers/db", "/var/lib/postgresql/data")
    .WithHostPort(5432)
    .AddDatabase("auroraflowboard");

builder.AddProject<Projects.Aurora_Flowboard_Api>("aurora-flowboard-api")
    .WithEnvironment("ConnectionStrings__Database", flowboardDatabase)
    .WithReference(flowboardDatabase)
    .WaitFor(flowboardDatabase);

await builder.Build().RunAsync();
