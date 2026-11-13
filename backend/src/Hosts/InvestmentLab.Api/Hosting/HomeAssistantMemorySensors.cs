using System.Globalization;
using System.Net.Http.Json;
using InvestmentLab.BuildingBlocks.Localization;
using InvestmentLab.SharedKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;

namespace InvestmentLab.Api.Hosting;

internal sealed class HomeAssistantMemorySensors(HttpClient http, ILogger<HomeAssistantMemorySensors> logger) : BaseExporter<Metric>
{
    public const string SupervisorAddress = "http://supervisor/";

    private const long BytesPerMebibyte = 1024 * 1024;

    private static readonly IReadOnlyList<MemorySensor> Sensors =
    [
        new(
            Metric: "dotnet.process.memory.working_set",
            EntityId: "sensor.investlab_api_memory",
            Name: Message.Of("sensor.api_memory"),
            Icon: "mdi:memory"),
        new(
            Metric: "dotnet.gc.last_collection.heap.size",
            EntityId: "sensor.investlab_api_managed_heap",
            Name: Message.Of("sensor.api_managed_heap"),
            Icon: "mdi:database"),
    ];

    public override ExportResult Export(in Batch<Metric> batch)
    {
        var readings = ReadingsOf(batch);
        if (readings.Count == 0)
            return ExportResult.Success;

        try
        {
            PublishAsync(readings).GetAwaiter().GetResult();

            return ExportResult.Success;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The memory of the API could not be published to Home Assistant");

            return ExportResult.Failure;
        }
    }

    private static Dictionary<MemorySensor, long> ReadingsOf(in Batch<Metric> batch)
    {
        var readings = new Dictionary<MemorySensor, long>();
        foreach (var metric in batch)
        {
            var sensor = Sensors.FirstOrDefault(sensor => sensor.Metric == metric.Name);
            if (sensor is not null)
                readings[sensor] = BytesOf(metric);
        }

        return readings;
    }

    private static long BytesOf(Metric metric)
    {
        var bytes = 0L;
        foreach (ref readonly var point in metric.GetMetricPoints())
            bytes += point.GetSumLong();

        return bytes;
    }

    private async Task PublishAsync(Dictionary<MemorySensor, long> readings)
    {
        var localizer = MessageLocalizer.In(await HomeAssistantLanguageAsync());

        foreach (var (sensor, bytes) in readings)
            await PublishStateAsync(sensor, bytes, localizer);
    }

    private async Task<Language> HomeAssistantLanguageAsync()
    {
        var config = await http.GetFromJsonAsync<HomeAssistantConfig>("core/api/config");

        return Language.OfCultureOrDefault(config?.Language ?? "");
    }

    private async Task PublishStateAsync(MemorySensor sensor, long bytes, MessageLocalizer localizer)
    {
        var mebibytes = bytes / BytesPerMebibyte;
        var state = new SensorState(
            State: mebibytes.ToString(CultureInfo.InvariantCulture),
            Attributes: new Dictionary<string, string>
            {
                ["friendly_name"] = localizer.Render(sensor.Name),
                ["unit_of_measurement"] = "MiB",
                ["device_class"] = "data_size",
                ["state_class"] = "measurement",
                ["icon"] = sensor.Icon,
            });

        using var response = await http.PostAsJsonAsync($"core/api/states/{sensor.EntityId}", state);
        response.EnsureSuccessStatusCode();
    }

    private sealed record MemorySensor(string Metric, string EntityId, string Name, string Icon);

    private sealed record SensorState(string State, Dictionary<string, string> Attributes);

    private sealed record HomeAssistantConfig(string? Language);
}
