using InvestmentLab.Rebalancing.Reports.Domain;
using System.Text.Json;

namespace InvestmentLab.Rebalancing.Reports.Application.GetReport;

public sealed record SectionScoreDto(
    Guid InstrumentId,
    string Name,
    string Group,
    decimal? Price,
    decimal? Momentum,
    decimal? Return1m,
    decimal? Return3m,
    decimal? Return12m,
    decimal? Volatility,
    bool? AboveMovingAverage,
    int? Rank,
    string Status,
    string Reason,
    IReadOnlyDictionary<string, JsonElement>? Values)
{
    public static SectionScoreDto From(SectionScore sectionScore) => new(
        InstrumentId: sectionScore.InstrumentId,
        Name: sectionScore.Name,
        Group: sectionScore.Group,
        Price: sectionScore.Price,
        Momentum: sectionScore.Momentum,
        Return1m: sectionScore.Return1m,
        Return3m: sectionScore.Return3m,
        Return12m: sectionScore.Return12m,
        Volatility: sectionScore.Volatility,
        AboveMovingAverage: sectionScore.AboveMovingAverage,
        Rank: sectionScore.Rank,
        Status: sectionScore.Status,
        Reason: sectionScore.Reason,
        Values: sectionScore.Values);
}
