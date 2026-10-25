var builder = DistributedApplication.CreateBuilder(args);

// Tests and CI pass InvestmentLab:EphemeralDatabase=true to get a throwaway Postgres.
var ephemeral = bool.TryParse(builder.Configuration["InvestmentLab:EphemeralDatabase"], out var flag) && flag;

var postgres = builder.AddPostgres("postgres");
if (!ephemeral)
{
    // Keep quotes and ledger between runs; the container survives AppHost restarts.
    postgres.WithDataVolume("investmentlab-postgres-data").WithLifetime(ContainerLifetime.Persistent);
}

var database = postgres.AddDatabase("investmentlab");

builder.AddProject<Projects.InvestmentLab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar (API docs)" });

builder.Build().Run();
