using System.Text.Json;
using InvestmentLab.Strategies.Contracts;
using InvestmentLab.Strategies.Definitions.Application;

namespace InvestmentLab.Strategies.Definitions.Infrastructure;

internal sealed class StrategyDirectory(IStrategyRepository strategies) : IStrategyDirectory
{
    public async Task<IReadOnlyList<StrategySummaryDto>> GetStrategies(CancellationToken cancellationToken) =>
        [.. (await strategies.ListAsync(cancellationToken)).Select(strategy => new StrategySummaryDto(
            strategy.Id.Value, strategy.Name, strategy.TypeCode, strategy.Latest.Number, strategy.Latest.Universe, strategy.Latest.CashInstrumentId,
            ReportingOf(strategy.Latest.Parameters)))];

    /// <summary>The "reporting" parameters of a strategy; versions made before they existed get the classic monthly + weekly.</summary>
    internal static StrategyReportingDto ReportingOf(string parameters)
    {
        try
        {
            using var document = JsonDocument.Parse(parameters);
            if (!document.RootElement.TryGetProperty("reporting", out var reporting) || reporting.ValueKind != JsonValueKind.Object)
                return StrategyReportingDto.Default;
            return new StrategyReportingDto(
                Capitalized(reporting, "rebalance") ?? StrategyReportingDto.Default.Rebalance,
                Capitalized(reporting, "riskReview") ?? StrategyReportingDto.Default.RiskReview);
        }
        catch (JsonException)
        {
            return StrategyReportingDto.Default;
        }
    }

    private static string? Capitalized(JsonElement reporting, string name) =>
        reporting.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text ? char.ToUpperInvariant(text[0]) + text[1..] : null;
}
