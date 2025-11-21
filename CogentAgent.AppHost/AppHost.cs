var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.CogentAgent_Web>("cogentagent-web");

builder.Build().Run();
