using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace InvestmentLab.Rebalancing.Shared.Infrastructure;

internal static class JsonColumn
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
    private static readonly JsonSerializerOptions TextEnumOptions = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> property, bool enumsAsText = false)
    {
        var options = enumsAsText ? TextEnumOptions : Options;
        var converter = new ValueConverter<T, string>(
            value => JsonSerializer.Serialize(value, options),
            json => JsonSerializer.Deserialize<T>(json, options)!);
        var comparer = new ValueComparer<T>(
            (left, right) => JsonSerializer.Serialize(left, options) == JsonSerializer.Serialize(right, options),
            value => JsonSerializer.Serialize(value, options).GetHashCode(),
            value => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, options), options)!);
        return property.HasConversion(converter, comparer).HasColumnType("jsonb");
    }
}
