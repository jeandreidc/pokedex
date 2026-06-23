var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var api = builder.AddProject<Projects.Kota_Pokedex_Api>("api")
    .WithReference(redis)
    .WithEnvironment("Cache__Provider", "Redis")
    .WithExternalHttpEndpoints();

builder.Build().Run();
