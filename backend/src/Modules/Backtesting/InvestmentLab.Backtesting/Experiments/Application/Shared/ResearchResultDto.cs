using InvestmentLab.Backtesting.Replays.Domain;

namespace InvestmentLab.Backtesting.Experiments.Application.Shared;

public sealed record ResearchResultDto(
    DateOnly Start,
    DateOnly End,
    string Currency,
    int Trades,
    int RiskExits,
    int SignalReports,
    IReadOnlyList<ResearchSeriesDto> Series,
    IReadOnlyList<ResearchJournalEntryDto>? Journal,
    decimal InitialCapital,
    decimal FinalCapital)
{
    public static ResearchResultDto From(ReplayResult replayResult) => new(
        Start: replayResult.Start,
        End: replayResult.End,
        Currency: replayResult.Currency,
        Trades: replayResult.Trades,
        RiskExits: replayResult.RiskExits,
        SignalReports: replayResult.SignalReports,
        Series: [.. replayResult.Series.Select(ResearchSeriesDto.From)],
        Journal: replayResult.Journal?.Select(ResearchJournalEntryDto.From).ToList(),
        InitialCapital: replayResult.InitialCapital,
        FinalCapital: replayResult.FinalCapital);
}
