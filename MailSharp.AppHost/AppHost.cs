var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.MailSharp_ApiService>("apiservice")
    .WithHttpHealthCheck("/health");

builder.AddProject<Projects.MailSharp_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddProject<Projects.MailSharp_Worker>("mailsharp-worker");

builder.Build().Run();
