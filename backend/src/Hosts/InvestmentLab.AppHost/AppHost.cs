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

// Python strategy engine (FastAPI). `uv sync` runs before start; code reloads on change.
var strategyEngine = builder.AddUvicornApp("strategy-engine", "../../../../strategy-engine", "finance_decisions.api.main:app")
    .WithUv()
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation { Url = "/docs", DisplayText = "Swagger (strategy engine)" });

var api = builder.AddProject<Projects.InvestmentLab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(strategyEngine)
    // Probe over http: CI agents do not trust the ASP.NET Core dev certificate.
    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar (API docs)" });

// Angular dev server; /api is proxied to the API so the browser sees one origin (cookie auth, SameSite=Strict).
// Tests and CI (ephemeral database) run without it.
if (!ephemeral)
{
    builder.AddJavaScriptApp("frontend", "../../../../frontend", "start")
        .WithReference(api)
        .WaitFor(api)
        .WithHttpEndpoint(port: 4200, env: "PORT")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
