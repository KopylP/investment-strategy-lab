var builder = DistributedApplication.CreateBuilder(args);

var ephemeral = bool.TryParse(builder.Configuration["InvestmentLab:EphemeralDatabase"], out var flag) && flag;

var postgres = builder.AddPostgres("postgres");
if (!ephemeral)
{
    postgres.WithDataVolume("investmentlab-postgres-data").WithLifetime(ContainerLifetime.Persistent);
}

var database = postgres.AddDatabase("investmentlab");

var strategyEngine = builder.AddUvicornApp("strategy-engine", "../../../../strategy-engine", "finance_decisions.api.main:app")
    .WithUv()
    .WithHttpHealthCheck("/health")
    .WithUrlForEndpoint("http", _ => new ResourceUrlAnnotation { Url = "/docs", DisplayText = "Swagger (strategy engine)" });

var api = builder.AddProject<Projects.InvestmentLab_Api>("api")
    .WithReference(database)
    .WaitFor(database)
    .WithReference(strategyEngine)

    .WithHttpHealthCheck("/health", endpointName: "http")
    .WithUrlForEndpoint("https", _ => new ResourceUrlAnnotation { Url = "/scalar", DisplayText = "Scalar (API docs)" });

if (!ephemeral)
{
    builder.AddJavaScriptApp("frontend", "../../../../frontend", "start")
        .WithNpm(install: false)
        .WithReference(api)
        .WaitFor(api)
        .WithHttpEndpoint(port: 4200, env: "PORT")
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
