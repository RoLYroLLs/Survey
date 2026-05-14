var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Survey_Web>("web")
	.WithExternalHttpEndpoints();

builder.Build().Run();
