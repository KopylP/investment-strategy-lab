using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvestmentLab.StrategyBuilder.Drafts.Infrastructure;

internal static class DraftJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
}
