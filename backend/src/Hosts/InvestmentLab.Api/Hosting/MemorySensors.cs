using System.Net.Http.Headers;
using OpenTelemetry.Metrics;

namespace InvestmentLab.Api.Hosting;

public static class MemorySensors
{
    private const string SupervisorTokenSetting = "SUPERVISOR_TOKEN";
    private const string HttpClientName = nameof(HomeAssistantMemorySensors);

    private static readonly TimeSpan PublishInterval = TimeSpan.FromMinutes(1);

    public static void AddMemorySensors(this WebApplicationBuilder builder)
    {
        if (builder.Configuration[SupervisorTokenSetting] is not { Length: > 0 } token)
            return;

        builder.Services.AddHttpClient(HttpClientName, http =>
        {
            http.BaseAddress = new Uri(HomeAssistantMemorySensors.SupervisorAddress);
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        });
        builder.Services.AddOpenTelemetry().WithMetrics(metrics => metrics.AddReader(PublishingReader));
    }

    private static MetricReader PublishingReader(IServiceProvider services)
    {
        var http = services.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
        var logger = services.GetRequiredService<ILogger<HomeAssistantMemorySensors>>();
        var sensors = new HomeAssistantMemorySensors(http, logger);

        return new PeriodicExportingMetricReader(sensors, (int)PublishInterval.TotalMilliseconds);
    }
}
