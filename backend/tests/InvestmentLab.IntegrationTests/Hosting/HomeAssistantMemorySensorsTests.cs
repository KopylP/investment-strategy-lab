extern alias api;

using System.Net;
using System.Text.Json;
using api::InvestmentLab.Api.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace InvestmentLab.IntegrationTests.Hosting;

public sealed class HomeAssistantMemorySensorsTests
{
    private readonly FakeHomeAssistant _homeAssistant = new();

    [Fact]
    public void The_memory_of_the_process_from_the_runtime_metrics_becomes_two_sensors_in_home_assistant()
    {
        PublishOnce();

        _homeAssistant.States.Keys.ShouldBe(["sensor.investlab_api_memory", "sensor.investlab_api_managed_heap"], ignoreOrder: true);
        _homeAssistant.States.Values.ShouldAllBe(state => int.Parse(state.GetProperty("state").GetString()!) > 0);
        _homeAssistant.States["sensor.investlab_api_memory"].GetProperty("attributes").GetProperty("unit_of_measurement").GetString().ShouldBe("MiB");
    }

    [Fact]
    public void The_sensors_are_named_in_the_language_of_home_assistant()
    {
        _homeAssistant.Language = "uk";

        PublishOnce();

        var name = _homeAssistant.States["sensor.investlab_api_memory"].GetProperty("attributes").GetProperty("friendly_name").GetString();
        name.ShouldBe("Investment Lab: пам’ять API");
    }

    private void PublishOnce()
    {
        var http = new HttpClient(_homeAssistant) { BaseAddress = new Uri(HomeAssistantMemorySensors.SupervisorAddress) };
        var sensors = new HomeAssistantMemorySensors(http, NullLogger<HomeAssistantMemorySensors>.Instance);
        using var meters = Sdk.CreateMeterProviderBuilder()
            .AddRuntimeInstrumentation()
            .AddReader(new BaseExportingMetricReader(sensors))
            .Build();

        GC.Collect();
        meters.ForceFlush();
    }

    private sealed class FakeHomeAssistant : HttpMessageHandler
    {
        private const string StatesPath = "/core/api/states/";

        public string Language { get; set; } = "en";

        public Dictionary<string, JsonElement> States { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (path == "/core/api/config")
                return Json(new { language = Language });

            if (path.StartsWith(StatesPath, StringComparison.Ordinal))
                States[path[StatesPath.Length..]] = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken)).RootElement;

            return Json(new { });
        }

        private static HttpResponseMessage Json(object body) =>
            new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json") };
    }
}
