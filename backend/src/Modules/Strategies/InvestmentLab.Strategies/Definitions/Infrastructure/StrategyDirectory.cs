using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyDirectory(IStrategyRepository strategies) : IStrategyDirectory
{
    public async Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken) =>
        [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => new StrategySummaryDto(
            strategy.Id.Value, strategy.Name, strategy.TypeCode, strategy.Latest.Number, strategy.Latest.Universe, strategy.Latest.CashInstrumentId,
            new StrategyReportingDto(strategy.Latest.Frequency.ToString(), RiskReviewOf(strategy.Latest.Parameters))))];

    /// <summary>The "reporting.riskReview" parameter; versions made before it existed keep the weekly review.</summary>
    internal static string RiskReviewOf(string parameters)
    {
        try
        {
            using var document = JsonDocument.Parse(parameters);
            return document.RootElement.TryGetProperty("reporting", out var reporting) && reporting.ValueKind == JsonValueKind.Object
                ? Capitalized(reporting, "riskReview") ?? StrategyReportingDto.Default.RiskReview
                : StrategyReportingDto.Default.RiskReview;
        }
        catch (JsonException)
        {
            return StrategyReportingDto.Default.RiskReview;
        }
    }

    private static string? Capitalized(JsonElement reporting, string name) =>
        reporting.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text ? char.ToUpperInvariant(text[0]) + text[1..] : null;
}
