using Aspire.Hosting;

[assembly: AssemblyFixture(typeof(InvestmentLab.IntegrationTests.AppHostFixture))]

namespace InvestmentLab.IntegrationTests;

public sealed class AppHostFixture : IAsyncLifetime
{
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(5);

    private DistributedApplication? _app;

    public DistributedApplication App => _app ?? throw new InvalidOperationException("AppHost is not started.");

    public async ValueTask InitializeAsync()
    {
        using var timeout = new CancellationTokenSource(StartupTimeout);

        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.InvestmentLab_AppHost>(
            ["--InvestmentLab:EphemeralDatabase=true"], timeout.Token);

        _app = await builder.BuildAsync(timeout.Token);
        await _app.StartAsync(timeout.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("api", timeout.Token);
        await _app.ResourceNotifications.WaitForResourceHealthyAsync("strategy-engine", timeout.Token);
    }

    public HttpClient CreateApiClient() => App.CreateHttpClient("api", "http");

    public HttpClient CreateStrategyEngineClient() => App.CreateHttpClient("strategy-engine", "http");

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }
}
