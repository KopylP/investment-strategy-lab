using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Signals.Monitoring.Infrastructure;

internal static class JsonbValue
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static ValueConverter<T, string> Converter<T>() => new(
        value => JsonSerializer.Serialize(value, Options),
        json => JsonSerializer.Deserialize<T>(json, Options)!);

    public static ValueComparer<T> Comparer<T>() => new(
        (left, right) => JsonSerializer.Serialize(left, Options) == JsonSerializer.Serialize(right, Options),
        value => JsonSerializer.Serialize(value, Options).GetHashCode(StringComparison.Ordinal),
        value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!);
}
